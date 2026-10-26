# App Store assets

Upload the JPEG screenshots in each device folder to the matching screenshot
group in App Store Connect. Keep each folder's numerical order when uploading.

| Folder | Capture device | Image size | Purpose |
|---|---|---:|---|
| `ios/iphone-6.3/` | iPhone 17 Pro | 1206 × 2622 | Required medium Dynamic Island iPhone group |
| `ios/iphone-6.9/` | iPhone 17 Pro Max | 1320 × 2868 | Large iPhone group |
| `ios/ipad-13/` | iPad Pro 13-inch (M5) | 2064 × 2752 | Required iPad group |

All screenshots are direct captures of the running app. The inventory is
fictional and uses reserved `example.test` service domains. The status bar is
set to 9:41. The screenshots are JPEGs without alpha channels.

`ios/app-icon-1024.png` is the 1024 × 1024 App Store icon from the iOS asset
catalog. It is already embedded in the app build; this copy is for reference.

## Product page creative

`ios/creative-assets/universal-5244x2950.png` is the universal creative asset
for the product page header and Search Results. It is a PNG at Apple's universal
asset size. `ios/creative-assets/header-3840x1646.jpg` is the alternate wide
header version. Use the universal asset first where App Store Connect offers
the same creative for both placements.

The App Store preview asset is a video slot and remains optional. The supplied
screenshots are ready to upload; they do not need a separate preview video.

## Screenshot order

1. `01-overview` — infrastructure summary
2. `02-inventory` — searchable resource list
3. `03-resource-detail` — hardware details and dependent systems
4. `04-edit-resource` — resource editor (iPhone only)
5. `05-service-detail` — service address and host relationship (large iPhone)
6. `06-containment-tree` — infrastructure relationships (large iPhone)

The medium iPhone set includes overview, inventory, resource detail, and
containment tree. The iPad set includes overview, inventory, and containment
tree.
