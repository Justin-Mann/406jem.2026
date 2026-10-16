# Pi Valve Controller

A Python app for a Raspberry Pi 4 that drives multiple SSR-41FDD (or similar)
solid-state relays to switch 12V valves, optionally automating them off water
sensor readings via a small rule engine. Runs headless as a systemd service
with a local web dashboard for live status and manual override.

## Hardware wiring notes

**Read your specific SSR module's datasheet before wiring anything** — the
notes below describe the common pattern for hobby SSR boards, but exact
turn-on voltage/current varies by model.

- **Control side (Pi → SSR):** Small SSR modules (SSR-25DA/40DA/40DD/FDD-style)
  typically accept a 3–32VDC control signal at a few mA, which a Pi GPIO pin
  (3.3V logic) can often drive directly — GPIO → SSR `+`, Pi GND → SSR `-`.
  Some modules need closer to 5V to turn on reliably from 3.3V logic; if a
  relay won't reliably switch, drive it through a small NPN transistor (or a
  premade opto-isolated relay driver board) instead of the GPIO pin directly.
- **Load side (SSR → valve):** Wire the 12V valve's supply through the SSR's
  output terminals. **Use a separate 12V supply for the valves** — do not
  power valves from the Pi's 5V rail. If the valve is inductive (a solenoid,
  which it is) and the SSR/module doesn't already include flyback protection,
  add a flyback diode across the valve coil.
- **Sensors:** three built-in sensor types (see Config below) cover the
  common cases:
  - `digital` — a float switch or threshold sensor that reads as a clean
    HIGH/LOW into a GPIO pin.
  - `pulse_counter` — a flow meter (e.g. YF-S201-style) with a pulse output
    into a GPIO pin; reports flow rate in liters/minute.
  - `ads1115` — an analog sensor (e.g. capacitive soil moisture, analog
    water-level) read through an ADS1115 I2C ADC breakout, since the Pi 4 has
    no native ADC.

Keep the Pi's GPIO/control wiring and the 12V valve wiring electrically
separate except through the SSR itself — don't share the Pi's ground with the
12V supply's ground unless the SSR module is opto-isolated and the datasheet
says it's safe to do so.

## Setup (on the Raspberry Pi)

```bash
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
cp config.example.yaml config.yaml   # then edit pins/sensors/rules for your wiring
python main.py --config config.yaml
```

Raspberry Pi OS Bookworm (Pi 4) uses `lgpio` as the `gpiozero` backend
(RPi.GPIO is deprecated on newer kernels); `requirements.txt` installs it
automatically on Linux only, so the same requirements file installs cleanly
on a non-Pi dev machine for running the test suite.

Then open `http://<pi-ip>:8000/` for the dashboard.

## Config (`config.yaml`)

```yaml
poll_interval_seconds: 5

relays:
  - id: valve_1
    name: "Front Yard Valve"
    pin: 17
    active_high: true       # GPIO HIGH energizes the SSR (module-dependent)
    default_state: false

sensors:
  - id: soil_moisture_1
    name: "Front Yard Moisture"
    type: ads1115
    channel: 0
    i2c_address: 0x48

  - id: tank_low_switch
    name: "Tank Low Float Switch"
    type: digital
    pin: 27
    pull_up: true            # idles HIGH, reads LOW when the switch triggers

  - id: flow_meter_1
    name: "Main Flow Meter"
    type: pulse_counter
    pin: 22
    pulses_per_liter: 450

rules:
  - relay_id: valve_1
    sensor_id: soil_moisture_1
    operator: "<"             # turn the relay ON when the sensor value < threshold
    threshold: 1.2
    hysteresis: 0.1           # avoids rapid on/off chatter near the threshold
    min_on_seconds: 30        # must stay ON at least this long before turning off
    min_off_seconds: 30       # must stay OFF at least this long before turning on
    enabled: true
```

A relay only follows its rule(s) while in **auto** mode. Toggling a relay from
the dashboard switches it to **manual** mode (automation for that relay
pauses); switch it back to auto from the dashboard to resume rule-based
control.

## Running as a service

```bash
sudo cp systemd/pi-valve-controller.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now pi-valve-controller
```

Edit the `WorkingDirectory`/`ExecStart` paths in the unit file first if the
project isn't checked out at `/home/pi/PiValveController`.

## Tests

```bash
pip install -r requirements-dev.txt
pytest
```

Tests run against `gpiozero`'s mock pin factory, so they work on any machine
(including Windows) without real GPIO hardware.
