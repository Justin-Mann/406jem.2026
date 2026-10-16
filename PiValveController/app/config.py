from dataclasses import dataclass, field

import yaml

_KNOWN_SENSOR_KEYS = {"id", "name", "type"}


@dataclass
class RelayConfig:
    id: str
    name: str
    pin: int
    active_high: bool = True
    default_state: bool = False


@dataclass
class SensorConfig:
    id: str
    name: str
    type: str
    options: dict = field(default_factory=dict)


@dataclass
class RuleConfig:
    relay_id: str
    sensor_id: str
    operator: str
    threshold: float
    hysteresis: float = 0.0
    min_on_seconds: float = 0.0
    min_off_seconds: float = 0.0
    enabled: bool = True


@dataclass
class AppConfig:
    poll_interval_seconds: float
    relays: list
    sensors: list
    rules: list


def load_config(path: str) -> AppConfig:
    with open(path, "r", encoding="utf-8") as f:
        raw = yaml.safe_load(f) or {}

    relays = [RelayConfig(**r) for r in raw.get("relays", [])]

    sensors = []
    for s in raw.get("sensors", []):
        options = {k: v for k, v in s.items() if k not in _KNOWN_SENSOR_KEYS}
        sensors.append(SensorConfig(id=s["id"], name=s["name"], type=s["type"], options=options))

    rules = [RuleConfig(**r) for r in raw.get("rules", [])]

    relay_ids = {r.id for r in relays}
    sensor_ids = {s.id for s in sensors}
    if len(relay_ids) != len(relays):
        raise ValueError("Duplicate relay ids in config")
    if len(sensor_ids) != len(sensors):
        raise ValueError("Duplicate sensor ids in config")
    for rule in rules:
        if rule.relay_id not in relay_ids:
            raise ValueError(f"Rule references unknown relay_id: {rule.relay_id}")
        if rule.sensor_id not in sensor_ids:
            raise ValueError(f"Rule references unknown sensor_id: {rule.sensor_id}")

    return AppConfig(
        poll_interval_seconds=float(raw.get("poll_interval_seconds", 5)),
        relays=relays,
        sensors=sensors,
        rules=rules,
    )
