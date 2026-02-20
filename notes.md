```
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
sudo mv bin/Release/net*/osx-arm64/publish/RackPeek /usr/local/bin/rpk
sudo chmod +x /usr/local/bin/rpk

```

```
cd vhs
brew install vhs
vhs ./rpk.tape

brew install imagemagick
brew install --cask google-chrome # if chrome not installed
chmod +x webui_capture.sh
./webui_capture.sh

```

```



```

```bash
# Manually build / push
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -f ./Dockerfile \
  -t aptacode/rackpeek:v0.0.11 \
  -t aptacode/rackpeek:latest \
  --push ..


```


e2e tests
```bash
cd Tests.E2e
dotnet tool install --global Microsoft.Playwright.CLI
dotnet build

playwright install

docker build -t rackpeek:ci -f RackPeek.Web/Dockerfile .


```