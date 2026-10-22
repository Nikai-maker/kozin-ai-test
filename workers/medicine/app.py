from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from transformers import AutoTokenizer, AutoModelForSequenceClassification, pipeline
import uvicorn
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="Medicine Worker")

# Load model at startup
classifier = None

class PredictionRequest(BaseModel):
    text: str

class PredictionResponse(BaseModel):
    label: str
    score: float

@app.on_event("startup")
async def load_model():
    global classifier
    logger.info("[MEDICINE] Loading model mrm8488/electricidad-small-finetuned-medical-diagnostics...")
    try:
        classifier = pipeline(
            "text-classification",
            model="mrm8488/electricidad-small-finetuned-medical-diagnostics",
            device=-1,  # CPU
        )
        logger.info("[MEDICINE] Model loaded successfully")
    except Exception as e:
        logger.error(f"[MEDICINE] Failed to load model: {e}")
        raise

@app.post("/predict", response_model=PredictionResponse)
async def predict(request: PredictionRequest):
    if classifier is None:
        raise HTTPException(status_code=503, detail="Model not loaded")

    logger.info(f"[MEDICINE] Received prediction request: {request.text[:100]}...")

    try:
        result = classifier(request.text, truncation=True, max_length=512)[0]

        label = result['label']
        score = result['score']

        logger.info(f"[MEDICINE] Prediction: {label} (score: {score:.3f})")
        logger.warning("[MEDICINE] DISCLAIMER: This is a demo model and should NOT be used for medical diagnosis")

        return PredictionResponse(label=label, score=score)
    except Exception as e:
        logger.error(f"[MEDICINE] Prediction failed: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/health")
async def health():
    return {"status": "ok", "model_loaded": classifier is not None}

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8003)
