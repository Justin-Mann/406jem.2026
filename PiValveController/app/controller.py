import logging
import threading

from .relay import Relay
from .rules import desired_relay_state
from .sensors import build_sensor

logger = logging.getLogger(__name__)


class SystemController:
    def __init__(self, config):
        self._config = config
        self._lock = threading.RLock()
        self._stop_event = threading.Event()

        self.relays = {
            r.id: Relay(r.id, r.name, r.pin, r.active_high, r.default_state) for r in config.relays
        }
        self.sensors = {s.id: build_sensor(s) for s in config.sensors}
        self.rules = config.rules
        self.sensor_values = {}

    def poll_once(self):
        with self._lock:
            for sensor_id, sensor in self.sensors.items():
                try:
                    self.sensor_values[sensor_id] = sensor.read()
                except Exception:
                    logger.exception("Failed to read sensor %s", sensor_id)

            for rule in self.rules:
                if not rule.enabled:
                    continue
                relay = self.relays.get(rule.relay_id)
                if relay is None or relay.mode != "auto":
                    continue
                value = self.sensor_values.get(rule.sensor_id)
                if value is None:
                    continue

                desired = desired_relay_state(rule, value, relay.is_on)
                if desired == relay.is_on:
                    continue

                # Require the relay to have held its current state for the
                # configured minimum before flipping it again, so a valve
                # doesn't chatter when a sensor value hovers near a threshold.
                min_seconds = rule.min_on_seconds if relay.is_on else rule.min_off_seconds
                if relay.seconds_in_state() < min_seconds:
                    continue

                logger.info("Rule (%s -> %s): setting relay to %s", rule.sensor_id, rule.relay_id, desired)
                relay.set(desired)

    def run_forever(self):
        while not self._stop_event.is_set():
            self.poll_once()
            self._stop_event.wait(self._config.poll_interval_seconds)

    def stop(self):
        self._stop_event.set()
        with self._lock:
            for relay in self.relays.values():
                relay.close()
            for sensor in self.sensors.values():
                sensor.close()

    def set_relay_mode(self, relay_id: str, mode: str):
        if mode not in ("auto", "manual"):
            raise ValueError("mode must be 'auto' or 'manual'")
        with self._lock:
            self.relays[relay_id].mode = mode

    def set_relay_state(self, relay_id: str, state: bool):
        with self._lock:
            relay = self.relays[relay_id]
            relay.mode = "manual"
            relay.set(state)

    def snapshot(self) -> dict:
        with self._lock:
            return {
                "relays": [
                    {
                        "id": r.id,
                        "name": r.name,
                        "is_on": r.is_on,
                        "mode": r.mode,
                        "seconds_in_state": round(r.seconds_in_state(), 1),
                    }
                    for r in self.relays.values()
                ],
                "sensors": [
                    {"id": sid, "name": sensor.name, "value": self.sensor_values.get(sid)}
                    for sid, sensor in self.sensors.items()
                ],
            }
