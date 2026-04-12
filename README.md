# ExpenseCategorizer
 

A demo-ready portfolio project built with **Blazor WebAssembly**, **Azure Functions**, **Cosmos DB**, **ML.NET**, and **Azure OpenAI**.  
It showcases dual-mode expense uploads, automated categorization, and human-in-the-loop correction.

---

## 🚀 Features

- **Dual Upload Modes**
  - **JSON Upload**: Strict schema, guaranteed accuracy.
  - **OCR Upload**: Extracts text from receipts/PDFs, predicts categories, and generates explanations.

- **Automated Categorization**
  - ML.NET predicts expense categories (Food, Travel, Utilities, Entertainment, etc.).
  - Azure OpenAI generates human-readable explanations for each categorization.

- **Human-in-the-Loop Correction**
  - Inline **Edit/Delete** buttons in the dashboard.
  - Rows with `Amount = 0` are highlighted for quick correction.
  - Users can fix OCR mistakes without losing entries.

- **Cloud-Native Architecture**
  - Blazor WebAssembly frontend.
  - Azure Functions backend APIs (`UploadExpense`, `GetExpenses`, `UpdateExpense`, `DeleteExpense`).
  - Cosmos DB for scalable storage.

---

## 📷 Demo Flow

1. Upload a **JSON file** → clean entries saved.
2. Upload a **PDF receipt** → OCR extracts items, categories predicted.
3. Dashboard shows all expenses → edit/delete available.
4. Rows with missing amounts (`0`) are flagged → user corrects them.

---

## 🛠️ Tech Stack

- **Frontend**: Blazor WebAssembly
- **Backend**: Azure Functions
- **Database**: Cosmos DB (partitioned by category)
- **AI Services**: ML.NET (categorization), Azure OpenAI (explanations)
- **OCR**: Azure Cognitive Services

---

## 📂 Project Structure

- `ExpenseCategorizer.Shared` → Models
- `ExpenseCategorizer.Client` → Blazor UI
- `ExpenseCategorizerFunction` → Azure Functions
- `DatabaseService.cs` → Cosmos DB integration

---

## ⚡ Getting Started

1. Clone the repo:
   ```bash
   git clone https://github.com/svkshailendra/ExpenseCategorizer.git

## 📸 Screenshots
![Home Page](docs/Home.png)  
![Upload Page](docs/upload.png)
![Dashboard](docs/dashboard.png)
![Report Page](docs/report.png)

## ⚙️ Setup
1. Install .NET 8 SDK and Azure Functions Core Tools.
2. Clone the repo:
   git clone https://github.com/svkshailendra/ExpenseCategorizer.git
3. Configure Cosmos DB connection string in local.settings.json.
4. Run the Blazor client:
   dotnet run --project ExpenseCategorizer.Client
5. Run the Functions backend:
   func start

## 🗺️ Architecture Overview

The system is composed of:

- **Blazor WebAssembly** frontend for UI.
- **Azure Functions** backend APIs (Upload, Get, Update, Delete).
- **OCR Service** (Azure Cognitive Services) for non‑JSON uploads.
- **ML.NET** for automated categorization.
- **Azure OpenAI** for generating explanations.
- **Cosmos DB** for scalable storage, partitioned by category.
- **Dashboard** for human‑in‑the‑loop correction and reporting.
![Architecture](docs/architecture.png)


## 🗺️ Roadmap
- Add charts for category totals and monthly trends.
- Multi-user authentication with Azure AD B2C.
- Export reports to CSV/Excel.
- Enhanced error handling with toast notifications.

