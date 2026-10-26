#!/usr/bin/env python3
"""
Regenerates RackPeek.Domain/Discovery/MacVendorTable.g.cs from the IEEE OUI registry.

    curl -sL -o /tmp/oui.csv https://standards-oui.ieee.org/oui/oui.csv
    ./generate-oui-table.py /tmp/oui.csv

The registry holds ~40,000 assignments; shipping all of them would bloat the
single-file binaries for little gain, so this keeps a curated subset: the vendors
whose hardware actually turns up on a homelab network. Add a pattern to VENDORS
below and re-run to extend it.

Records are packed as fixed-width "PPPPPPv" (6 hex prefix chars + one vendor index
char) in a sorted string so the lookup is a binary search with no allocation and no
dictionary to build at startup.
"""
import csv
import re
import sys

# Display name -> pattern matched against the registry's Organization Name.
# Anchored where a loose substring would drag in unrelated companies.
VENDORS = {
    "Apple": r"^Apple, Inc\.?$|^Apple Inc",
    "Intel": r"^Intel Corporate$|^Intel Corporation$",
    "Ubiquiti": r"Ubiquiti",
    "Raspberry Pi": r"Raspberry Pi",
    "Espressif": r"Espressif|^Shanghai High-Flying|Ai-Thinker",
    "Proxmox": r"Proxmox",
    "VMware": r"VMware",
    "QEMU/KVM": r"^QEMU",
    "VirtualBox": r"PCS Systemtechnik|VirtualBox",
    "Microsoft": r"^Microsoft Corporation$",
    "Parallels": r"^Parallels",
    "Synology": r"Synology",
    "QNAP": r"QNAP",
    "Netgear": r"NETGEAR|Netgear",
    "TP-Link": r"TP-LINK|TP-Link|Tp-Link",
    "D-Link": r"D-Link|D\-LINK",
    "MikroTik": r"MikroTik|Mikrotik|Routerboard",
    "Cisco": r"^Cisco Systems|^Cisco$",
    "Aruba": r"Aruba",
    "Zyxel": r"ZyXEL|Zyxel",
    "Realtek": r"Realtek",
    "Broadcom": r"^Broadcom",
    "NVIDIA": r"NVIDIA|Nvidia",
    "AMD": r"^Advanced Micro Devices",
    "Dell": r"^Dell Inc|^Dell Computer|^Dell EMC",
    "HPE/HP": r"Hewlett Packard|^HP Inc",
    "Supermicro": r"Super Micro|Supermicro",
    "ASUS": r"^ASUSTek|^ASUS",
    "ASRock": r"ASRock",
    "Gigabyte": r"GIGA-BYTE|Gigabyte",
    "MSI": r"Micro-Star",
    "Lenovo": r"^Lenovo",
    "Samsung": r"^Samsung Electro|^Samsung Electronics",
    "LG": r"^LG Electronics",
    "Sony": r"^Sony ",
    "Google": r"^Google, Inc|^Google LLC",
    "Amazon": r"^Amazon Technologies",
    "Sonos": r"^Sonos",
    "Signify (Hue)": r"Signify|Philips Lighting",
    "Philips": r"^Philips",
    "Xiaomi": r"Xiaomi|XIAOMI",
    "IKEA": r"IKEA|Inter IKEA",
    "Aqara": r"Lumi United|Aqara",
    "Reolink": r"Reolink",
    "Hikvision": r"Hikvision|HIKVISION",
    "Dahua": r"Dahua",
    "Axis": r"^Axis Communication",
    "AVM (Fritz!Box)": r"^AVM ",
    "Juniper": r"^Juniper",
    "Fortinet": r"^Fortinet",
    "Netgate": r"Netgate",
    "Seagate": r"^Seagate",
    "Western Digital": r"Western Digital",
    "Buffalo": r"^BUFFALO|^Buffalo",
    "Asustor": r"ASUSTOR|Asustor",
    "TerraMaster": r"TerraMaster|Terra-?Master",
    "Sophos": r"^Sophos",
    "Arris/CommScope": r"^ARRIS|CommScope",
    "Technicolor": r"Technicolor",
    "Sagemcom": r"Sagemcom",
    "Roku": r"^Roku",
    "Nintendo": r"^Nintendo",
    "Texas Instruments": r"^Texas Instruments",
    "Tuya": r"^Tuya|Hangzhou Tuya",
    "Shelly": r"Allterco|Shelly",
    "Zotac": r"ZOTAC",
    "Beelink": r"Beelink|AZW",
    "Minisforum": r"Minisforum|MINIX",
    "Ring": r"^Ring LLC|Ring Inc",
    "Arlo": r"^Arlo",
    "Wyze": r"^Wyze",
    "Sonoff": r"ITEAD|SONOFF",
    "Mellanox": r"Mellanox",
    "Chelsio": r"Chelsio",
    "Aquantia": r"Aquantia",
    "Edimax": r"Edimax|EDIMAX",
    "Tenda": r"^Tenda|Shenzhen Tenda",
    "Huawei": r"^HUAWEI|^Huawei",
    "Sercomm": r"^Sercomm",
    "Silicon Labs": r"Silicon Laborator",
    "Nordic Semiconductor": r"Nordic Semiconductor",
    "Pine64": r"^Pine ?64|PINE64",
    "Hardkernel (ODROID)": r"Hardkernel",
    "Radxa": r"^Radxa|Rockchip",
    "FriendlyELEC": r"FriendlyARM|FriendlyELEC",
    "Khadas": r"Khadas|Shenzhen Wesion",
}

# Prefixes that identify software but are not IEEE assignments, because hypervisors
# mint addresses out of the locally-administered range rather than buying an OUI.
# Without these a KVM guest would be reported as merely "randomised".
EXTRAS = {
    "525400": "QEMU/KVM",
    "00163E": "Xen",
}

# Vendor index is stored as one printable char, so the table cannot exceed this.
_FIRST_INDEX_CHAR = 33  # '!'
_MAX_VENDORS = 126 - _FIRST_INDEX_CHAR


def main(csv_path: str) -> int:
    names = sorted(set(VENDORS) | set(EXTRAS.values()))

    if len(names) > _MAX_VENDORS:
        sys.exit(f"{len(names)} vendors exceeds the {_MAX_VENDORS} a single index char can address.")

    index_of = {name: i for i, name in enumerate(names)}
    compiled = [(re.compile(pat), name) for name, pat in VENDORS.items()]

    seen: dict[str, str] = {}

    with open(csv_path, encoding="utf-8", errors="replace") as handle:
        for row in csv.DictReader(handle):
            prefix = (row.get("Assignment") or "").strip().upper()
            org = (row.get("Organization Name") or "").strip()

            if len(prefix) != 6 or prefix in seen:
                continue

            for pattern, name in compiled:
                if pattern.search(org):
                    seen[prefix] = name
                    break

    seen.update(EXTRAS)

    records = "".join(
        prefix + chr(_FIRST_INDEX_CHAR + index_of[name])
        for prefix, name in sorted(seen.items())
    )

    # 140 is a whole number of records, so each source line holds exactly 20 of them.
    chunks = [records[i:i + 140] for i in range(0, len(records), 140)]

    # The index alphabet starts at '!' and runs past '"' and '\', both of which have to
    # be escaped in the C# literal. Escaping changes only the source text; the string
    # the runtime builds still holds the original characters, so offsets stay correct.
    def escape(chunk: str) -> str:
        return chunk.replace("\\", "\\\\").replace('"', '\\"')

    literal = "\n".join(f'        "{escape(chunk)}" +' for chunk in chunks)
    literal = literal.rstrip(" +") + ";"

    vendor_literal = ",\n".join(f'        "{name}"' for name in names)

    out = f'''// <auto-generated>
//     Generated by generate-oui-table.py from the IEEE OUI registry
//     (https://standards-oui.ieee.org/oui/oui.csv). Do not edit by hand — add a
//     vendor pattern to the generator and re-run it instead.
//
//     {len(seen)} assignments across {len(names)} vendors, packed as fixed-width
//     "PPPPPPv" records (6 hex prefix chars + one vendor index char), sorted so
//     {'MacVendorLookup'} can binary-search them without building a dictionary.
// </auto-generated>

namespace RackPeek.Domain.Discovery;

internal static class MacVendorTable {{
    internal const int RecordLength = 7;

    internal const char FirstIndexChar = '{chr(_FIRST_INDEX_CHAR)}';

    internal static readonly string[] Vendors = [
{vendor_literal}
    ];

    internal static readonly string Records =
{literal}
}}
'''

    target = "RackPeek.Domain/Discovery/MacVendorTable.g.cs"

    with open(target, "w", encoding="utf-8") as handle:
        handle.write(out)

    print(f"Wrote {target}: {len(seen)} assignments, {len(names)} vendors, {len(records)} chars.")
    return 0


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit(__doc__)

    raise SystemExit(main(sys.argv[1]))
