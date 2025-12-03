-----

# Observatório Saúde

O **Observatório Saúde** é uma API RESTful desenvolvida para monitorar, agregar e fornecer inteligência de dados sobre estabelecimentos de saúde e capacidade hospitalar (leitos). A aplicação suporta a visualização geoespacial e a exportação de relatórios detalhados.

## 🚀 Tecnologias

  * **Backend:** .NET (C\#)
  * **API:** ASP.NET Core Web API
  * **Containerização:** Docker & Docker Compose
  * **Dados e Scripts:** Python (Auxiliar)
  * **Testes:** xUnit/NUnit
  * **Deployment:** GCP (Google Cloud)
  * **Banco de dados:** Postgresql (Google Cloud)

## 🔌 API Endpoints

A API está versionada (`v1`) e organizada nos seguintes contextos principais:

### 🏥 Estabelecimentos

Gerenciamento e consulta de dados sobre unidades de saúde, incluindo geolocalização e exportação.

| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `GET` | `/api/v1/Estabelecimento` | Lista o número de estabelecimentos cadastrados. |
| `GET` | `/api/v1/Estabelecimento/uf` | Lista o número de estabelecimentos e população pos Unidade Federativa. |
| `GET` | `/api/v1/Estabelecimento/info` | Retorna metadados ou resumos estatísticos dos estabelecimentos (suporta paginação/filtros). |
| `GET` | `/api/v1/Estabelecimento/geojson` | Retorna dados em formato **GeoJSON** para plotagem em mapas interativos. |
| `GET` | `/api/v1/Estabelecimento/export` | Download de listagem simplificada de estabelecimentos (CSV/XLSX). |
| `GET` | `/api/v1/Estabelecimento/export-details`| Download de relatório completo detalhado de estabelecimentos (CSV/XLSX). |

### 🛏️ Leitos (Capacidade Hospitalar)

Indicadores de ocupação e distribuição de leitos.

| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `GET` | `/api/v1/Leitos` | Listagem geral da capacidade de leitos por extabelecimento (suporta paginação/filtros). |
| `GET` | `/api/v1/Leitos/detalhes` | Dados granulares sobre tipos de leitos e capacidade de leitos (suporta paginação/filtros). |
| `GET` | `/api/v1/Leitos/indicadores` | KPIs gerais de ocupação. |
| `GET` | `/api/v1/Leitos/indicadores-por-estado` | Indicadores agrupados por UF. |
| `GET` | `/api/v1/Leitos/indicadores-por-regiao` | Indicadores agrupados por macro-regiões. |

### 🩺 System Health

Monitoramento da saúde da aplicação.

| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `GET` | `/api/v1/Health` | Health Check (Liveness/Readiness probe). |

-----

## ⚙️ Como Executar

### Pré-requisitos

  * [Docker](https://www.docker.com/) instalado.

### Passo a Passo

1.  **Clone o repositório:**

    ```bash
    git clone https://github.com/eDusVx/observatorio-saude.git
    cd observatorio-saude
    ```

2.  **Suba o ambiente com Docker Compose:**

    ```bash
    docker-compose up -d --build
    ```

3.  **Acesse a Documentação (Swagger):**
    Após a inicialização, a documentação interativa estará disponível em:

      * `http://localhost:<PORTA>/swagger`

## 🧪 Testes

O projeto conta com uma camada de testes automatizados para garantir a integridade dos indicadores.

```bash
dotnet test
```

## 📄 Licença

Este projeto está sob a licença MIT. Consulte o arquivo [LICENSE](https://www.google.com/search?q=LICENSE) para mais detalhes.

-----
