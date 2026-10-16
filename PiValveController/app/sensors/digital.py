from gpiozero import DigitalInputDevice

from .base import Sensor


class DigitalGpioSensor(Sensor):
    """A float switch or threshold sensor read as a clean HIGH/LOW."""

    def __init__(self, id, name, pin, pull_up=True):
        super().__init__(id, name)
        self._device = DigitalInputDevice(pin, pull_up=pull_up)

    def read(self) -> float:
        return float(self._device.value)

    def close(self):
        self._device.close()
