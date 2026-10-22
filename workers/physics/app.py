from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from setfit import SetFitModel
import uvicorn
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="Physics Worker")

# Load model at startup
model = None

class PredictionRequest(BaseModel):
    text: str

class PredictionResponse(BaseModel):
    label: str
    score: float

@app.on_event("startup")
async def load_model():
    global model
    logger.info("[PHYSICS] Loading model COURSEMO/physics-classifier-model...")
    try:
        model = SetFitModel.from_pretrained("COURSEMO/physics-classifier-model")
        logger.info("[PHYSICS] Model loaded successfully")
    except Exception as e:
        logger.error(f"[PHYSICS] Failed to load model: {e}")
        raise

@app.post("/predict", response_model=PredictionResponse)
async def predict(request: PredictionRequest):
    if model is None:
        raise HTTPException(status_code=503, message="Model not loaded")

    logger.info(f"[PHYSICS] Received prediction request: {request.text[:100]}...")

    try:
        # SetFit returns predictions
        predictions = model.predict([request.text])
        probs = model.predict_proba([request.text])

        label = predictions[0]
        score = float(probs[0].max())

        logger.info(f"[PHYSICS] Prediction: {label} (score: {score:.3f})")

        return PredictionResponse(label=label, score=score)
    except Exception as e:
        logger.error(f"[PHYSICS] Prediction failed: {e}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/health")
async def health():
    return {"status": "ok", "model_loaded": model is not None}

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8001)
