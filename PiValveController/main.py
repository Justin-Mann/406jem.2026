import argparse
import logging
import threading

import uvicorn

from app.config import load_config
from app.controller import SystemController
from app.web import create_app


def main():
    parser = argparse.ArgumentParser(description="Pi Valve Controller")
    parser.add_argument("--config", default="config.yaml")
    parser.add_argument("--host", default="0.0.0.0")
    parser.add_argument("--port", type=int, default=8000)
    args = parser.parse_args()

    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s: %(message)s")

    config = load_config(args.config)
    controller = SystemController(config)

    worker = threading.Thread(target=controller.run_forever, daemon=True)
    worker.start()

    app = create_app(controller)
    try:
        uvicorn.run(app, host=args.host, port=args.port)
    finally:
        controller.stop()


if __name__ == "__main__":
    main()
