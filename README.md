# VanderPan

Sistema de distribuição de produtos de panificação, composto por API, painel web e aplicativo móvel .NET MAUI.

## Pré-requisitos

- Windows com .NET 10 SDK.
- Para o app Android: workload MAUI/Android, Android SDK e um emulador ou dispositivo conectado.
- A API usa SQLite por padrão e cria/popula o banco `vanderpan.db` na primeira execução.

Confira a instalação do .NET e os workloads:

```powershell
dotnet --version
dotnet workload list
```

Se o workload Android estiver faltando, instale-o com:

```powershell
dotnet workload install maui-android
```

## API e painel web

Abra dois terminais na raiz do repositório.

No primeiro, inicie a API:

```powershell
dotnet run --project .\src\VanderPan.Api\VanderPan.Api.csproj --launch-profile http
```

A API fica em `http://localhost:5000`. Em desenvolvimento, suas rotas públicas de status e documentação são:

- `http://localhost:5000/` — status da API.
- `http://localhost:5000/health` — verificação de saúde.
- `http://localhost:5000/openapi/v1.json` — documento OpenAPI.

No segundo terminal, inicie o painel:

```powershell
dotnet run --project .\src\VanderPan.Web\VanderPan.Web.csproj --launch-profile http
```

Abra `http://localhost:5100` no navegador. A API precisa continuar rodando enquanto o painel estiver em uso.

### Contas de demonstração

O painel usa a conta administrativa:

- E-mail: `admin@vanderpan.com.br`
- Senha: `admin123`

O app móvel usa uma conta de vendedor seed:

- E-mail: `carlos@vanderpan.com.br`
- Senha: `123456`

Também existe o vendedor `mariana@vanderpan.com.br`, com a mesma senha `123456`. Essas credenciais são apenas para desenvolvimento local.

## App Android

Inicie um emulador pelo Android Studio ou, se o AVD de demonstração estiver configurado, pelo PowerShell:

```powershell
Start-Process "C:\Program Files (x86)\Android\android-sdk\emulator\emulator.exe" -ArgumentList "-avd pixel_7_-_api_36_0"
```

Confirme que o Android está conectado:

```powershell
$adb = "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"
& $adb devices
```

Com a API em execução no primeiro terminal e o emulador listado como `device`, instale e inicie o app:

```powershell
dotnet build .\src\VanderPan.App\VanderPan.App.csproj -t:Run -f net10.0-android
```

O app para Android Emulator acessa a API por `http://10.0.2.2:5000`. O manifesto Android permite tráfego HTTP para esse ambiente local; use HTTPS e ajuste a configuração de rede antes de distribuir o app.

## Compilar os projetos

Na raiz do repositório:

```powershell
dotnet build .\src\VanderPan.Api\VanderPan.Api.csproj
dotnet build .\src\VanderPan.Web\VanderPan.Web.csproj
dotnet build .\src\VanderPan.App\VanderPan.App.csproj -f net10.0-android
```
