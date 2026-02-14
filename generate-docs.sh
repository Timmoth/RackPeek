#!/usr/bin/env bash
set -euo pipefail

PROJECT_PATH="."
OUTPUT_FILE="COMMANDS.md"
declare -A VISITED

TREE_TEMP=$(mktemp)
BODY_TEMP=$(mktemp)

trap 'rm -f "$TREE_TEMP" "$BODY_TEMP"' EXIT

# ----------------------------
# Helpers
# ----------------------------

strip_colors() {
  sed -E "s/\x1B\[[0-9;]*[mK]//g"
}

run_help() {
 local project="./RackPeek" 
 local config="Release" 
 local publish_dir="$project/publish" 
 local exe="$publish_dir/RackPeek"

  if [[ ! -x "$exe" ]]; then
    echo "Publishing RackPeek ($config)..." >&2
    dotnet publish "$project" -c "$config" -o "$publish_dir" --self-contained false -p:PublishSingleFile=true >&2
  fi

  local output
  output=$("$exe" "$@" 2>&1 | strip_colors)
  echo "$output"
}

# UPDATED: Handles descriptions that start on the next line
get_description() {
  local help_output="$1"
  echo "$help_output" | awk '
    # If we find the header...
    /^DESCRIPTION:/ {
      in_desc = 1
      # Remove the header itself
      sub(/^DESCRIPTION:[[:space:]]*/, "")
      
      # If there is text on this same line, print it and quit
      if (length($0) > 0) {
        print $0
        exit
      }
      next
    }

    # If we are inside the description block...
    in_desc {
      # Stop if we hit an empty line or the next Section (UPPERCASE:)
      if (/^[[:space:]]*$/) exit
      if (/^[A-Z]+:/) exit

      # Trim leading/trailing whitespace
      gsub(/^[[:space:]]+|[[:space:]]+$/, "")
      
      # Print the line (we only take the first non-empty line for the index)
      print $0
      exit
    }
  '
}

# UPDATED: Handles "add <name>" correctly by taking only the first column ($1)
get_child_commands() {
  local help_output="$1"
  
  echo "$help_output" | awk '
    BEGIN { in_commands = 0 }
    /^COMMANDS:/ { in_commands = 1; next }
    
    in_commands {
      # Stop if we hit an empty line or a new section
      if ($0 ~ /^[[:space:]]*$/) exit;
      if ($0 ~ /^[A-Z]+:/) exit;

      # Match lines that look like commands (indented)
      if ($0 ~ /^[[:space:]]+[a-zA-Z0-9-]+/) {
        # Print only the first word (the command name)
        print $1
      }
    }
  '
}

# ----------------------------
# Recursion
# ----------------------------

generate_help_recursive() {
  local path_string="$1"
  
  # Convert string to array for the run_help command
  local current_cmd_array=()
  if [[ -n "$path_string" ]]; then
    IFS=' ' read -r -a current_cmd_array <<< "$path_string"
  fi

  local map_key="${path_string:-root}"
  
  if [[ -n "${VISITED["$map_key"]:-}" ]]; then return; fi
  VISITED["$map_key"]=1

  # 1. Run Help
  local help_output
  if ! help_output=$(run_help "${current_cmd_array[@]}" --help); then
    echo "Skipping: $map_key (help failed)" >&2
    return
  fi

if is_invalid_help "$help_output"; then
  echo "Skipping: $map_key (invalid command)" >&2
  return
fi


  # 2. Extract Description
  local description
  description=$(get_description "$help_output")
  
  # 3. Build Tree Entry
  local depth=${#current_cmd_array[@]}
  local indent=""
  [[ $depth -gt 0 ]] && printf -v indent "%*s" $((depth * 2)) ""
  
  local tree_label
  [[ $depth -eq 0 ]] && tree_label="rpk" || tree_label="${current_cmd_array[-1]}"

  local anchor_text="rpk $path_string"
  anchor_text="${anchor_text% }" 
  local anchor_link=$(echo "$anchor_text" | tr '[:upper:]' '[:lower:]' | tr ' ' '-')

  # Format: - [label](link) - Description
  local tree_entry="${indent}- [${tree_label}](docs/Commands.md#${anchor_link})"
  if [[ -n "$description" ]]; then
    tree_entry="${tree_entry} - ${description}"
  fi
  
  echo "$tree_entry" >> "$TREE_TEMP"

  # 4. Write Body
  local display_header
  [[ -z "$path_string" ]] && display_header="rpk" || display_header="rpk $path_string"

  {
    echo "## \`${display_header}\`"
    echo '```'
    echo "$help_output"
    echo '```'
    echo ""
  } >> "$BODY_TEMP"

  # 5. Recurse
  local child_cmds
  mapfile -t child_cmds < <(get_child_commands "$help_output")

  for child in "${child_cmds[@]}"; do
    echo "Recursing into: ${display_header} ${child}" >&2
    local next_path
    if [[ -z "$path_string" ]]; then
      next_path="$child"
    else
      next_path="$path_string $child"
    fi
    generate_help_recursive "$next_path"
  done
}

is_invalid_help() {
  local output="$1"

  # Detect common failure patterns
  if echo "$output" | grep -qiE \
    "Unknown command|CommandParseException|Unexpected error occurred"; then
    return 0
  fi

  return 1
}


# ----------------------------
# Main
# ----------------------------

echo "Generating documentation..."

generate_help_recursive ""

{
  echo ""
  cat "$TREE_TEMP"
} > "Shared.Rcl/wwwroot/raw_docs/CommandIndex.md"

{
  echo "# CLI Commands"
  echo ""
  cat "$BODY_TEMP"
} > "Shared.Rcl/wwwroot/raw_docs/Commands.md"

echo "Generated Successfully."