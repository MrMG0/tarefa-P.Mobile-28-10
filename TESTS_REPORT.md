# Relatório de Testes — VanderPan

**Projeto:** VanderPan — API, painel web e aplicativo móvel  
**Data das verificações:** 28/10/2026  
**Ambiente:** Windows x64, .NET 10 e emulador Android Pixel 7 (API 36)

## 1. Builds

| Componente | Comando | Resultado |
|---|---|---|
| API | `dotnet build .\src\VanderPan.Api\VanderPan.Api.csproj` | ✅ Compilou para .NET 10, sem erros. |
| Painel web | `dotnet build .\src\VanderPan.Web\VanderPan.Web.csproj` | ✅ Compilou para .NET 10, sem erros. |
| App Android | `dotnet build .\src\VanderPan.App\VanderPan.App.csproj -f net10.0-android` | ✅ Compilou, sem erros. |
| Instalação e inicialização Android | `dotnet build .\src\VanderPan.App\VanderPan.App.csproj -t:Run -f net10.0-android` | ✅ Instalou e iniciou o app no emulador. |

## 2. Testes HTTP da API

As validações de regras de negócio que alteram estoque e títulos foram realizadas com um banco SQLite temporário.

| ID | Cenário | Resultado |
|---|---|---|
| API-01 | Acessar rota protegida sem token | ✅ HTTP 401. |
| API-02 | Login administrativo e consulta de dados administrativos | ✅ Login e acesso autorizados. |
| API-03 | Login de vendedor e consulta à própria carteira | ✅ Acesso autorizado. |
| API-04 | Vendedor consultar a carteira de outro vendedor | ✅ HTTP 403. |
| API-05 | Vendedor chamar rota exclusiva de administrador | ✅ HTTP 403. |
| API-06 | Criar pedido, aplicar corte e faturar | ✅ Venda gerada com duas parcelas no cenário testado. |
| API-07 | Tentar faturar o mesmo pedido novamente | ✅ HTTP 409; venda não duplicada. |
| API-08 | Cancelar venda antes do envio | ✅ Estoque restaurado de 249 para 250 e parcelas canceladas no cenário testado. |
| API-09 | Consultar vendedor pela API | ✅ Resposta não contém a propriedade `SenhaHash`. |
| API-10 | Consultar `GET /`, `GET /health` e `GET /openapi/v1.json` em desenvolvimento | ✅ As três rotas responderam HTTP 200. |

## 3. Testes manuais do app Android

Os testes foram feitos no emulador, com a API disponível em `http://localhost:5000`. No emulador, o app acessa o host por `http://10.0.2.2:5000`.

| ID | Cenário | Resultado |
|---|---|---|
| APP-01 | Iniciar o app e abrir a tela de acesso | ✅ Tela de login exibida sem encerramento inesperado. |
| APP-02 | Entrar com o vendedor de demonstração | ✅ Login aceito e roteiro carregado. |
| APP-03 | Carregar o roteiro de visitas | ✅ Clientes exibidos por sequência de visita. |
| APP-04 | Abrir detalhes de cliente | ✅ Dados cadastrais, posição financeira e histórico exibidos. |
| APP-05 | Abrir o catálogo | ✅ Categorias, produtos, preços e estoque exibidos. |
| APP-06 | Adicionar produto ao carrinho | ✅ Produto adicionado e total atualizado corretamente. |
| APP-07 | Abrir a lista e os detalhes de pedidos | ✅ Pedido existente, itens e status exibidos. |
| APP-08 | Abrir o formulário de novo cliente | ✅ Formulário e campos exibidos. |
| APP-09 | Encerrar a sessão | ✅ App retornou à tela de login. |

## 4. Testes do painel web

| ID | Cenário | Resultado |
|---|---|---|
| WEB-01 | Compilar o painel para .NET 10 | ✅ Build concluído sem erros. |
| WEB-02 | Acessar `http://localhost:5100` com o painel iniciado | ✅ Resposta HTTP 200. |

## 5. Comandos de build

Na raiz do repositório, os builds podem ser executados com:

```powershell
dotnet build .\src\VanderPan.Api\VanderPan.Api.csproj
dotnet build .\src\VanderPan.Web\VanderPan.Web.csproj
dotnet build .\src\VanderPan.App\VanderPan.App.csproj -f net10.0-android
```
