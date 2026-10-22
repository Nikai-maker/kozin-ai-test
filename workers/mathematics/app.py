from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from transformers import AutoTokenizer, AutoModelForSequenceClassification, pipeline
from collections import Counter
import uvicorn
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="Mathematics Worker with Voting")

# Load three instances of the same model
models = []

class PredictionRequest(BaseModel):
    text: str

class ModelPrediction(BaseModel):
    model: str
    label: str
    score: float

class VotingResponse(BaseModel):
    final_label: str
    votes: dict[str, int]
    predictions: list[ModelPrediction]

@app.on_event("startup")
async def load_models():
    global models
    model_name = "lschlessinger/bert-finetuned-math-prob-classification"

    logger.info("[MATHEMATICS] Loading 3 instances of the model for voting...")

    try:
        for i in range(3):
            logger.info(f"[MATHEMATICS] Loading model instance {i+1}...")
            classifier = pipeline(
                "text-classification",
                model=model_name,
                device=-1,  # CPU
                top_k=None  # Get all class probabilities
            )
            models.append({
                "id": f"model_{i+1}",
                "classifier": classifier
            })

        logger.info("[MATHEMATICS] All 3 model instances loaded successfully")
        logger.info("[MATHEMATICS] Note: Using same checkpoint as fallback (see README)")
    except Exception as e:
        logger.error(f"[MATHEMATICS] Failed to load models: {e}")
        raise

@app.post("/predict", response_model=VotingResponse)
async def predict(request: PredictionRequest):
    if not models:
        raise HTTPException(status_code=503, detail="Models not loaded")

    logger.info(f"[MATHEMATICS] Received prediction request: {request.text[:100]}...")

    predictions = []
    labels = []

    # Get predictions from all 3 models
    for model_info in models:
        model_id = model_info["id"]
        classifier = model_info["classifier"]

        logger.info(f"[MATHEMATICS] Calling {model_id}")

        try:
            result = classifier(request.text, truncation=True, max_length=512)

            # Get top prediction
            top_prediction = max(result[0], key=lambda x: x['score'])
            label = top_prediction['label']
            score = top_prediction['score']

            predictions.append(ModelPrediction(
                model=model_id,
                label=label,
                score=score
            ))
            labels.append(label)

            logger.info(f"[MATHEMATICS]     {model_id} -> {label} (score: {score:.3f})")

        except Exception as e:
            logger.error(f"[MATHEMATICS] {model_id} prediction failed: {e}")
            raise HTTPException(status_code=500, detail=f"{model_id} failed: {str(e)}")

    # Majority voting
    vote_counter = Counter(labels)
    votes_dict = dict(vote_counter)
    final_label = vote_counter.most_common(1)[0][0]

    logger.info(f"[MATHEMATICS] Predictions:")
    for pred in predictions:
        logger.info(f"[MATHEMATICS]     {pred.model} -> {pred.label}")

    logger.info(f"[MATHEMATICS] Voting result: {final_label} ({votes_dict[final_label]}/{len(models)})")

    return VotingResponse(
        final_label=final_label,
        votes=votes_dict,
        predictions=predictions
    )

@app.get("/health")
async def health():
    return {"status": "ok", "models_loaded": len(models)}

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8002)
