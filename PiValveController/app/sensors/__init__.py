from .base import Sensor
from .digital import DigitalGpioSensor
from .pulse import PulseCounterSensor

_SENSOR_TYPES = {
    "digital": DigitalGpioSensor,
    "pulse_counter": PulseCounterSensor,
}


def build_sensor(cfg) -> Sensor:
    if cfg.type == "ads1115":
        # imported lazily since it needs adafruit-blinka, which is only
        # installable on real I2C-capable hardware (not on a dev machine)
        from .ads1115 import ADS1115Sensor

        cls = ADS1115Sensor
    else:
        cls = _SENSOR_TYPES.get(cfg.type)
        if cls is None:
            raise ValueError(f"Unknown sensor type: {cfg.type}")

    return cls(id=cfg.id, name=cfg.name, **cfg.options)
