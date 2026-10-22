# AI Orchestrator MVP

Учебный проект, демонстрирующий паттерны **Orchestrator-Workers** и **Voting** с использованием LLM-оркестратора на C# и ML-воркеров на Python.

## Архитектура

```text
User
  |
  v
C# LLM Orchestrator (Ollama API)
  |
  |-- Tool Selection (JSON)
  |
  +---- Physics Worker (port 8001) --------> COURSEMO/physics-classifier-model
  |
  +---- Mathematics Worker (port 8002) ----> 3x lschlessinger/bert-finetuned-math-prob-classification
  |                                          |
  |                                          +---> Majority Voting
  |
  +---- Medicine Worker (port 8003) -------> mrm8488/electricidad-small-finetuned-medical-diagnostics
  |
  v
C# LLM Orchestrator (Final Answer)
  |
  v
User
```

## Описание паттернов

### Orchestrator Pattern

LLM-оркестратор (C#) принимает запрос пользователя и:

1. Анализирует запрос через локальную LLM (Ollama)
2. Выбирает подходящий инструмент (physics/mathematics/medicine)
3. Отправляет запрос соответствующему worker'у через HTTP
4. Получает результат от worker'а
5. Формирует финальный ответ пользователю через LLM

Оркестратор не содержит бизнес-логики предметных областей, он только координирует выполнение задач специализированными воркерами.

### Voting Pattern

В Mathematics Worker реализован **majority voting**:

1. Три экземпляра одной модели получают один запрос
2. Каждая модель возвращает свой класс (Algebra, Geometry, и т.д.)
3. C#-код агрегирует результаты через подсчёт голосов
4. Выбирается класс с максимальным количеством голосов

**Важное примечание**: Из-за сложности поиска трёх независимых готовых математических моделей с совместимыми классами, используется один checkpoint `lschlessinger/bert-finetuned-math-prob-classification`, загруженный как три независимых instance. Это fallback-решение для демонстрации voting-механизма.

## ML-модели

Все модели загружаются с Hugging Face и запускаются локально на CPU:

### Physics Worker

- **Модель**: [COURSEMO/physics-classifier-model](https://huggingface.co/COURSEMO/physics-classifier-model)
- **Тип**: SetFit text classification
- **Размер**: ~0.1B параметров (109M)
- **Классы**: `wave`, `forces`, `electricity`, `magnetism`, `nuclear`, `thermal`
- **Точность**: 93.88%

### Mathematics Worker (с Voting)

- **Модель**: [lschlessinger/bert-finetuned-math-prob-classification](https://huggingface.co/lschlessinger/bert-finetuned-math-prob-classification)
- **Тип**: BERT fine-tuned для классификации математических задач
- **Размер**: ~0.1B параметров
- **Классы**: `Counting & Probability`, `Prealgebra`, `Algebra`, `Number Theory`, `Geometry`, `Intermediate Algebra`, `Precalculus`
- **Точность**: 79%
- **Количество**: 3 независимых экземпляра для voting

### Medicine Worker

- **Модель**: [mrm8488/electricidad-small-finetuned-medical-diagnostics](https://huggingface.co/mrm8488/electricidad-small-finetuned-medical-diagnostics)
- **Тип**: ELECTRA (small), дообученная классификация медицинских текстов
- **Язык**: испанский (`lang: es` в карточке модели)
- **Метки**: `LABEL_0` / `LABEL_1`. Расшифровка не опубликована ни в `config.json`, ни в карточке модели
- **Точность**: не указана автором модели

**ПРЕДУПРЕЖДЕНИЕ**: Medicine Worker - это демонстрационная ML-модель. Результаты **НЕ предназначены для медицинских диагнозов** и не являются заменой профессиональной медицинской консультации.

## Требования

### Система

- **ОС**: Windows, Linux или macOS
- **RAM**: минимум 8 GB (рекомендуется для локального запуска всех компонентов)
- **CPU**: Любой современный CPU (модели работают на CPU, GPU не требуется)

### Программное обеспечение

1. **.NET 8 SDK**
   - Скачать: https://dotnet.microsoft.com/download/dotnet/8.0

2. **Python 3.11+**
   - Скачать: https://www.python.org/downloads/

3. **Ollama**
   - Скачать: https://ollama.ai/download
   - Модель: `llama3.2:3b` (компактная instruct-модель для 8 GB RAM)

4. **Docker и Docker Compose** (опционально)
   - Для запуска workers в контейнерах
   - Скачать: https://www.docker.com/products/docker-desktop

## Установка

### 1. Клонирование репозитория

```bash
git clone <repository-url>
cd kozin-project-cs
```

### 2. Установка Ollama и модели

```bash
# Установите Ollama с официального сайта
# Затем загрузите модель:
ollama pull llama3.2:3b
```

### 3. Установка зависимостей Python для workers

```bash
# Physics Worker
cd workers/physics
pip install -r requirements.txt

# Mathematics Worker
cd ../mathematics
pip install -r requirements.txt

# Medicine Worker
cd ../medicine
pip install -r requirements.txt

cd ../..
```

### 4. Сборка C# Orchestrator

```bash
cd orchestrator
dotnet restore
dotnet build
cd ..
```

## Запуск

### Вариант 1: Ручной запуск (рекомендуется для отладки)

Откройте **4 терминала**:

**Терминал 1: Ollama**
```bash
ollama serve
```

**Терминал 2: Physics Worker**
```bash
cd workers/physics
python app.py
# Запустится на http://localhost:8001
```

**Терминал 3: Mathematics Worker**
```bash
cd workers/mathematics
python app.py
# Запустится на http://localhost:8002
```

**Терминал 4: Medicine Worker**
```bash
cd workers/medicine
python app.py
# Запустится на http://localhost:8003
```

**Терминал 5: C# Orchestrator**
```bash
cd orchestrator
dotnet run
```

### Вариант 2: Docker Compose (workers)

```bash
# Запустите workers через Docker Compose
docker compose up --build

# В отдельном терминале запустите orchestrator
cd orchestrator
dotnet run
```

**Примечание**: Ollama нужно запускать отдельно, так как он используется напрямую на хост-машине.

## Использование

После запуска всех компонентов вы увидите CLI-интерфейс:

```text
=== AI Orchestrator MVP ===
Orchestrator-Workers pattern with LLM routing and Voting

[ORCHESTRATOR] Checking Ollama connection...
[ORCHESTRATOR] Ollama connected
[ORCHESTRATOR] Available tools: physics, mathematics, medicine

Type your question or 'exit' to quit.

User: 
```

### Пример запроса

#### Пример: Physics

```text
User: What branch of physics studies magnets?

[ORCHESTRATOR] User request: What branch of physics studies magnets?
[ORCHESTRATOR] Selected tool: physics
[PHYSICS] Calling worker at http://localhost:8001
[PHYSICS] Prediction: magnetism (score: 0.956)
[ORCHESTRATOR] Sending worker result to LLM
[ORCHESTRATOR] Final answer: The branch of physics that studies magnets is magnetism.