# RackPeek Mobile

RackPeek Mobile is the iOS and Android app for viewing and editing a RackPeek inventory. It works with a local inventory when you are offline or have not connected a server. When you connect, you can upload your local changes or discard them in favor of the server inventory.

## Run locally

Install Flutter and the platform tools for your device, then from this directory run:

```sh
flutter pub get
flutter run
```

Run tests and static analysis with:

```sh
flutter test
flutter analyze
```

## Release builds

Set the app version and build number in `pubspec.yaml`. Increment the build number for every store upload. Build an Android App Bundle with `flutter build appbundle --release`; build an iOS archive with Xcode or `flutter build ipa`.

## Privacy and store assets

The [privacy policy](privacy-policy.md) explains local storage and server sync. Store icons, screenshots, and listing graphics are in [`store-assets/`](store-assets/).
