#!/usr/bin/env bash
set -euo pipefail

PROJECT_PATH="."
OUTPUT_FILE="COMMANDS.md"
declare -A VISITED

# Create temp files for the Tree structure and the Body content
TREE_TEMP=$(mktemp)
BODY_TEMP=$(mktemp)

# Ensure temp files are cleaned up when script exits
trap 'rm -f "$TREE_TEMP" "$BODY_TEMP"' EXIT

# ----------------------------
# Helpers
# ----------------------------

strip_colors() {
  sed -r "s/\x1B\[[0-9;]*[mK]//g"
}

run_help() {
  local project="./RackPeek"
  local runtime="osx-arm64"
  local config="Release"
  local publish_dir="$project/bin/$config/net10.0/$runtime/publish"
  local exe="$publish_dir/RackPeek"

  if [[ ! -x "$exe" ]]; then
    echo "Publishing RackPeek ($config)..." >&2
    if ! dotnet publish "$project" \
        -c "$config" \
        -r "$runtime" \
        --self-contained false \
        -p:PublishSingleFile=true; then
      echo "ERROR: dotnet publish failed" >&2
      return 1
    fi
  fi

  echo "Running: $exe $*" >&2

  local output
  if ! output=$("$exe" "$@" 2>&1 | strip_colors); then
    echo "WARNING: command failed: $exe $*" >&2
    echo "$output" >&2
    return 1
  fi

  echo "$output"
}



get_commands() {
  local help_output="$1"

  echo "$help_output" | awk '
    BEGIN { in_commands = 0 }

    /^COMMANDS:/ { in_commands = 1; next }

    in_commands {
      if ($0 ~ /^[[:space:]]{4}[a-zA-Z0-9-]+[[:space:]]+/) {
        print $1
        next
      }
      if ($0 !~ /^[[:space:]]*$/) {
        exit
      }
    }
  '
}

# ----------------------------
# Recursion
# ----------------------------

generate_help_recursive() {
  local current_path=("$@")
  
  local flat_cmd="${current_path[*]}"
  local map_key="${flat_cmd:-root}"

  # --- TREE GENERATION LOGIC ---
  # 1. Calculate depth for indentation (2 spaces per level)
  local depth=${#current_path[@]}
  local indent=""
  if [[ $depth -gt 0 ]]; then
    printf -v indent "%*s" $((depth * 2)) ""
  fi

  # 2. Determine the display label for the tree (Leaf name only)
  local tree_label
  if [[ $depth -eq 0 ]]; then
    tree_label="rpk"
  else
    tree_label="${current_path[-1]}" # Last element of array
  fi

  # 3. Create a clean anchor link for Markdown (lowercase, replace spaces with dashes)
  #    e.g., "rpk switches list" -> "#rpk-switches-list"
  local anchor_text="rpk $flat_cmd"
  # Trim leading space if flat_cmd was empty
  anchor_text="${anchor_text% }" 
  local anchor_link
  anchor_link=$(echo "$anchor_text" | tr '[:upper:]' '[:lower:]' | tr ' ' '-')

  # 4. Append to Tree Temp File
  echo "${indent}- [${tree_label}](Commands.md#${anchor_link})" >> "$TREE_TEMP"
  # -----------------------------

  if [[ -n "${VISITED["$map_key"]:-}" ]]; then
    return
  fi
  VISITED["$map_key"]=1

  local help_output
  if ! help_output=$(run_help "${current_path[@]}" --help); then
    echo "Skipping: $map_key (help failed)" >&2
    return
  fi

  # --- BODY GENERATION ---
  # Determine header title
  local display_header
  if [[ -z "$flat_cmd" ]]; then
    display_header="rpk"
  else
    display_header="rpk $flat_cmd"
  fi

  {
    echo "## \`${display_header}\`"
    echo '```'
    echo "$help_output"
    echo '```'
    echo ""
  } >> "$BODY_TEMP"
  # -----------------------

  local commands
  mapfile -t commands < <(get_commands "$help_output")

  for cmd in "${commands[@]}"; do
    echo "Recursing into: ${display_header} ${cmd}" >&2
    generate_help_recursive "${current_path[@]}" "$cmd"
  done
}

# ----------------------------
# Main
# ----------------------------

echo "Generating documentation..."

# Start recursion
generate_help_recursive

# Write command index
{
  echo "# CLI Command Index"
  echo ""
  cat "$TREE_TEMP"
} > "CommandIndex.md"

# Write full command documentation
{
  echo "# CLI Commands"
  echo ""
  cat "$BODY_TEMP"
} > "Commands.md"


echo "Generated $OUTPUT_FILE successfully."