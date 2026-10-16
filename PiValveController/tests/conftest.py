import os

# Must be set before gpiozero is imported anywhere so tests run without real
# GPIO hardware (works on the Pi's dev machine too, e.g. Windows).
os.environ.setdefault("GPIOZERO_PIN_FACTORY", "mock")
