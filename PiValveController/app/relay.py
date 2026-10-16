import time

from gpiozero import DigitalOutputDevice


class Relay:
    def __init__(self, id, name, pin, active_high=True, default_state=False):
        self.id = id
        self.name = name
        self.mode = "auto"  # or "manual" — set by the dashboard on override
        self._device = DigitalOutputDevice(pin, active_high=active_high, initial_value=default_state)
        self._last_changed = time.monotonic()

    @property
    def is_on(self) -> bool:
        return bool(self._device.value)

    def seconds_in_state(self) -> float:
        return time.monotonic() - self._last_changed

    def set(self, state: bool):
        if state == self.is_on:
            return
        if state:
            self._device.on()
        else:
            self._device.off()
        self._last_changed = time.monotonic()

    def close(self):
        self._device.close()
