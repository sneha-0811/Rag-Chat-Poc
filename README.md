# RAG POC — AI Document Q&A System

A minimal Retrieval-Augmented Generation (RAG) proof of concept that lets you ask questions against local documents using Cohere (embeddings), Groq/Llama (answers), and Qdrant (vector storage).

---

## What is RAG?

RAG (Retrieval-Augmented Generation) is a technique where instead of relying solely on an LLM's training data, you:

1. Store your own documents as vectors in a vector database
2. When a user asks a question, find the most relevant document chunks
3. Send those chunks along with the question to the LLM
4. The LLM answers using YOUR data — not generic knowledge

---

## Architecture

```
+------------------+        +-------------------+        +----------------------+
|                  |        |                   |        |                      |
|   docs/ folder   +------> |  .NET Web API     +------> |  Cohere API          |
|   (.txt files)   |        |  (localhost:5001) |        |  embed-english-      |
|                  |        |                   |        |  light-v3.0          |
+------------------+        +--------+----------+        |  (Embeddings only)   |
                                      |                  +----------------------+
                                      v
                            +-------------------+        +----------------------+
                            |                   |        |                      |
                            |  Qdrant           |        |  Groq API            |
                            |  Vector DB        |        |  llama-3.3-70b-      |
                            |  (Docker :6333)   |        |  versatile           |
                            |                   |        |  (Answers only)      |
                            +-------------------+        +----------------------+
                                      ^                           ^
                                      |                           |
+------------------+        +-------------------+                |
|                  |        |                   +----------------+
|  frontend/       +------> |  .NET Web API     |
|  index.html      |        |  (localhost:5001) |
|  (Chat UI)       |        |                   |
+------------------+        +-------------------+
```

---

## Models — Who Does What

| Role | Provider | Model | Purpose |
|---|---|---|---|
| Embeddings | Cohere | `embed-english-light-v3.0` | Converts text to 384-dim vectors for storage and search |
| LLM / Answers | Groq | `llama-3.3-70b-versatile` | Reads retrieved chunks and generates a natural language answer |
| Vector Storage | Qdrant | — | Stores vectors and runs similarity search |

### Why two providers?
Embedding models and LLM models are fundamentally different. Almost no provider offers both for free. Cohere handles embeddings (fast, free, 100 req/min) and Groq handles answers (free, 30 req/min, very fast inference).

---

## Phase 1 — Ingestion (Run Once)

```
docs/ folder
    |
    v
Read .txt files
    |
    v
Split into chunks (~500 words)
    |
    v
[Cohere] embed-english-light-v3.0
Embed each chunk  -->  [0.23, -0.87, 0.45 ... 384 numbers]
    |
    v
Store vectors + original text in Qdrant
```

## Phase 2 — Query (Every User Message)

```
User types a question
    |
    v
[Cohere] embed-english-light-v3.0
Embed the question  -->  [0.21, -0.85, 0.41 ... 384 numbers]
    |
    v
Search Qdrant --> returns top 3 matching text chunks
    |
    v
Build prompt:
    "Answer this question: {question}
     Using only this context: {chunk1} {chunk2} {chunk3}"
    |
    v
[Groq] llama-3.3-70b-versatile
Send prompt --> generate answer
    |
    v
Return answer to frontend UI
```

---

## Project Structure

```
RAG-POC/
├── backend/                         .NET 10 Web API
│   ├── Controllers/
│   │   ├── IngestController.cs      POST /api/ingest  - reads docs, chunks, embeds, stores
│   │   └── ChatController.cs        POST /api/chat    - takes question, returns answer
│   ├── Services/
│   │   ├── EmbeddingService.cs      calls Cohere embed-english-light-v3.0
│   │   ├── ChunkingService.cs       splits text into ~500 word chunks
│   │   ├── QdrantService.cs         stores and searches 384-dim vectors
│   │   └── ChatService.cs           builds prompt and calls Groq llama-3.3-70b
│   ├── appsettings.json             API keys and config (NOT committed to git)
│   ├── appsettings.example.json     template with placeholder values (safe to commit)
│   └── Program.cs                   app entry point, DI setup
│
├── docs/                            source documents (add your .txt files here)
│   ├── leave-policy.txt
│   ├── expense-policy.txt
│   └── it-support.txt
│
├── frontend/
│   └── index.html                   plain HTML + JS chat UI
│
├── docker-compose.yml               runs Qdrant vector DB
└── README.md
```

---

## Libraries & SDKs Used

### Qdrant.Client (v1.18.1)
Official .NET SDK for Qdrant vector database.
- Creates collections, stores vectors (upsert), runs cosine similarity search
- Communicates over gRPC (port 6334)
- NuGet: `Qdrant.Client`

### Cohere REST API (via HttpClient)
Called directly via .NET's built-in `HttpClient` — no SDK needed.
- Model: `embed-english-light-v3.0`
- Produces 384-dimensional float vectors
- Endpoint: `POST https://api.cohere.ai/v1/embed`
- Free tier: 100 requests/min

### Groq REST API (via HttpClient)
OpenAI-compatible API, called via `HttpClient` — no SDK needed.
- Model: `llama-3.3-70b-versatile` (Meta's Llama 3.3 70B)
- Endpoint: `POST https://api.groq.com/openai/v1/chat/completions`
- Free tier: 30 requests/min, very fast inference

### .NET 10 Web API
Standard ASP.NET Core Web API. No extra frameworks — just controllers and dependency injection.

---

## Prerequisites

| Tool | Version | Download |
|---|---|---|
| .NET SDK | 10.0 | https://dotnet.microsoft.com/download |
| Docker Desktop | Latest | https://www.docker.com/products/docker-desktop |
| Cohere Account | Free | https://dashboard.cohere.com |
| Groq Account | Free | https://console.groq.com |

---

## Step 1 — Get API Keys (Both Free)

### Cohere (Embeddings)
1. Go to **https://dashboard.cohere.com**
2. Sign up with your email
3. Go to **API Keys** → copy the default key

### Groq (LLM Answers)
1. Go to **https://console.groq.com**
2. Sign up → go to **API Keys** → **Create API Key**
3. Copy the key (starts with `gsk_`)

---

## Step 2 — Configure API Keys

Copy `backend/appsettings.example.json` to `backend/appsettings.json` and fill in your keys:

```json
{
  "Cohere": {
    "ApiKey": "PASTE_COHERE_KEY_HERE",
    "EmbeddingModel": "embed-english-light-v3.0"
  },
  "Groq": {
    "ApiKey": "PASTE_GROQ_KEY_HERE",
    "ChatModel": "llama-3.3-70b-versatile"
  },
  "Qdrant": {
    "Host": "localhost",
    "Port": 6334,
    "CollectionName": "documents"
  },
  "DocsPath": "../docs"
}
```

> `appsettings.json` is in `.gitignore` — your keys will never be committed.

---

## Step 3 — Start Qdrant (Vector Database)

Open a terminal in the project root folder:

```bash
docker-compose up -d
```

Verify it is running:
```bash
docker ps
```

Qdrant dashboard available at: `http://localhost:6333/dashboard`

---

## Step 4 — Start the Backend

```bash
cd backend
dotnet run
```

You should see:
```
Now listening on: http://localhost:5001
Application started.
```

---

## Step 5 — Open the Chat UI

Open `frontend/index.html` directly in your browser (double-click from File Explorer).

---

## Step 6 — Ingest Documents

1. Click **"Ingest Documents"** in the UI
2. Wait for the success message
3. This reads all `.txt` files from `docs/`, chunks them, generates embeddings via Cohere, and stores them in Qdrant
4. **Only needs to be done once** — data persists in Docker volume

---

## Step 7 — Ask Questions

Type a question and press Enter.

```
How many annual leave days do employees get?
What is the meal allowance for domestic travel?
How do I request a software installation?
When can I get a laptop replacement?
```

---

## Stopping the App

```bash
# Stop backend: Ctrl+C in the backend terminal

# Stop Qdrant
docker-compose down
```

> Qdrant data persists in a Docker named volume (`qdrant_data`). When you restart, skip the Ingest step.

---

## Restarting the App

```bash
docker-compose up -d    # start Qdrant
cd backend
dotnet run              # start backend
# open frontend/index.html — skip Ingest, go straight to chat
```

---

## Adding Your Own Documents

1. Add `.txt` files to the `docs/` folder
2. Click **"Ingest Documents"** in the UI
3. Ask questions about the new content

---

## Rate Limits (Free Tier)

| Provider | Model | Limit |
|---|---|---|
| Cohere | `embed-english-light-v3.0` | 100 req/min |
| Groq | `llama-3.3-70b-versatile` | 30 req/min |

---

## Future Enhancements (Beyond POC)

- Replace `docs/` folder with SharePoint via Microsoft Graph API
- Add conversation history / multi-turn chat
- Swap plain HTML frontend with Angular
- Add authentication
- Move to Azure with Azure AI Search instead of Qdrant
