import threading
import time

from gpiozero import DigitalInputDevice

from .base import Sensor


class PulseCounterSensor(Sensor):
    """A flow meter with a pulse output (e.g. YF-S201-style).

    read() reports the flow rate in liters/minute averaged over the time
    since the previous read() call, then resets the pulse count — so poll
    it on a regular interval (the controller's poll loop does this).
    """

    def __init__(self, id, name, pin, pulses_per_liter=450, pull_up=True):
        super().__init__(id, name)
        self._pulses_per_liter = pulses_per_liter
        self._count = 0
        self._lock = threading.Lock()
        self._last_read_time = time.monotonic()
        self._device = DigitalInputDevice(pin, pull_up=pull_up)
        self._device.when_activated = self._on_pulse

    def _on_pulse(self):
        with self._lock:
            self._count += 1

    def read(self) -> float:
        now = time.monotonic()
        with self._lock:
            count = self._count
            self._count = 0
        elapsed = max(now - self._last_read_time, 1e-6)
        self._last_read_time = now
        liters = count / self._pulses_per_liter
        return (liters / elapsed) * 60.0

    def close(self):
        self._device.close()
