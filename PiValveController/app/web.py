from pathlib import Path

from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import HTMLResponse
from fastapi.templating import Jinja2Templates
from pydantic import BaseModel

TEMPLATES_DIR = Path(__file__).parent / "templates"


class RelayCommand(BaseModel):
    mode: str | None = None
    state: bool | None = None


def create_app(controller) -> FastAPI:
    app = FastAPI(title="Pi Valve Controller")
    templates = Jinja2Templates(directory=str(TEMPLATES_DIR))

    @app.get("/", response_class=HTMLResponse)
    def dashboard(request: Request):
        return templates.TemplateResponse(request, "dashboard.html")

    @app.get("/api/state")
    def api_state():
        return controller.snapshot()

    @app.post("/api/relays/{relay_id}")
    def api_relay_command(relay_id: str, command: RelayCommand):
        if relay_id not in controller.relays:
            raise HTTPException(status_code=404, detail="Unknown relay")

        if command.state is not None:
            controller.set_relay_state(relay_id, command.state)
        elif command.mode is not None:
            controller.set_relay_mode(relay_id, command.mode)
        else:
            raise HTTPException(status_code=400, detail="Provide 'state' or 'mode'")

        return controller.snapshot()

    return app
