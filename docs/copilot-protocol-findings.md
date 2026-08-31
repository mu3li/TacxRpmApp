# Tacx Protocol Findings

## Summary

The two external references are highly relevant. Together they indicate that the Tacx NEO 2T is likely controlled through **ANT+ FE-C tunneled over Bluetooth Low Energy**, rather than through standard Bluetooth FTMS.

This is an evidence-based direction, not yet a confirmed result for this specific trainer and firmware. The next implementation should validate one command on the physical NEO 2T before connecting it to the app presets.

## Sources

- [pycycling](https://github.com/zacharyedwardbull/pycycling): Python BLE cycling library. Its README specifically lists the Tacx NEO and NEO 2T as tested with **ANT+ FE-C over BLE**.
- [pycycling Tacx Trainer Control implementation](https://github.com/zacharyedwardbull/pycycling/blob/master/pycycling/tacx_trainer_control.py): executable reference for service UUIDs, FE-C packet construction, resistance commands, and notifications.
- [BleTrainerControl](https://github.com/jedla22/BleTrainerControl): older Tacx demo application and the accompanying [How-to FE-C over BLE PDF](https://github.com/jedla22/BleTrainerControl/blob/master/How-to%20FE-C%20over%20BLE%20v1_0_0.pdf).
- [SHIFTR](https://github.com/JuergenLeber/SHIFTR): newer ESP32 BLE-to-network bridge, explicitly tested with a Garmin/Tacx NEO 2T. It includes FE-C pass-through and an FTMS emulation layer that translates FTMS commands into FE-C commands.

The BleTrainerControl project describes itself as a demo that sends and receives ANT payloads through a BLE tunnel. Its documentation is old, so it is useful protocol evidence but must be tested against current hardware and firmware.

## Important Difference From FTMS

The current app initially looked for:

```text
FTMS service:       00001826-0000-1000-8000-00805f9b34fb
FTMS Control Point: 00002ad9-0000-1000-8000-00805f9b34fb
```

Those UUIDs are standard FTMS. The external references indicate that older Tacx NEO trainers commonly expose the Tacx FE-C BLE tunnel instead. FTMS and FE-C are different protocols and their command packets must not be mixed.

The current app already recognizes the Tacx service and write characteristic, but it is missing the receive/notification characteristic and has not implemented FE-C packet writes.

## Tacx FE-C BLE UUIDs

```text
Tacx service:              6e40fec1-b5a3-f393-e0a9-e50e24dcca9e
FE-C write characteristic: 6e40fec3-b5a3-f393-e0a9-e50e24dcca9e
FE-C notify characteristic:6e40fec2-b5a3-f393-e0a9-e50e24dcca9e
```

The naming in the reference implementation is from the BLE client perspective:

- `fec3` is used to send FE-C packets to the trainer.
- `fec2` is used to receive FE-C notifications from the trainer.

The older BleTrainerControl documentation also mentions another service UUID:

```text
669aa305-0c08-969e-e211-86ad5062675f
```

The physical device scan should record whether this service appears on the NEO 2T being tested.

## Command Evidence

The `pycycling` implementation supports several FE-C operations. Relevant examples include:

- Basic resistance: FE-C command `0x30`
- Target power / ERG mode: FE-C command `0x31`
- Wind resistance: FE-C command `0x32`
- Track/simulation resistance: FE-C command `0x33`
- NEO-specific modes: command `0xFC`
- Requesting data pages: command `0x46`

For basic resistance, the reference implementation documents a range of 0 to 200 newtons. It constructs an ANT-style FE-C message beginning with:

```text
A4 09 4F 05 30 ...
```

The resistance is encoded as the final data byte in the command payload, then a checksum is appended. The checksum used by the reference implementation is the sum of the message bytes after the initial sync byte, masked to one byte:

```text
checksum = sum(message[1:]) & 0xFF
```

The exact packet must be reimplemented carefully in C# and tested with a low-risk fixed value. Do not assume the old FTMS packet `[0x04, lowByte, highByte]` applies to this FE-C path.

### Packet variant conflict

The references do not agree on every low-level field, so the packet format is not yet settled:

- `pycycling` shows FE-C command messages with message ID `0x4F` and calculates the checksum with a byte sum masked to 8 bits.
- SHIFTR's NEO 2T implementation shows messages with message ID `0x4E` and calculates the checksum using XOR across all message bytes.
- SHIFTR's basic-resistance method accepts a byte described as a resistance percentage/value from `0` to `200`. Its documentation explains that NEO 2T basic resistance is represented in `0.5%` increments and that the trainer's maximum resistance is read from FE-C capabilities.

This is a crucial reason to capture the real trainer's notifications and validate one command before wiring the feature into the app. We should not copy the checksum or message ID from one reference without determining which variant the NEO 2T firmware accepts.

## Notifications And Data Pages

The reference implementation enables notifications on `fec2` and parses several FE-C data pages:

- Page `16`: general FE data
- Page `25`: specific trainer data
- Page `71`: command status
- Page `80`: manufacturer identification
- Page `81`: product information

Page `71` is especially important for validation because it reports the last received command and a status such as:

- success
- fail
- not supported
- rejected
- uninitialized

The first real command implementation should enable notifications and capture page `71` before treating the command as successful.

## Recommended Implementation Order

1. Add the `fec2` notification characteristic to `TacxNeoService`.
2. After service discovery, verify that `fec3` is writable and `fec2` supports notifications.
3. Enable notifications on `fec2`.
4. Implement a small FE-C packet builder with explicit byte arrays and checksum calculation.
5. Send one fixed basic-resistance command using a conservative value.
6. Capture and log the notification response, especially command-status page `71`.
7. Verify that the trainer physically changes resistance.
8. Add validation for the supported resistance range.
9. Only then connect the command to Uphill, Cruise, and Downhill.

SHIFTR also demonstrates a useful architecture for later work: keep the trainer-side FE-C transport in the service, and treat FTMS as a separate adapter only if another client or future UI needs it. For this app, the simplest first implementation remains direct FE-C from `TacxNeoService`.

## What Must Still Be Confirmed

- Whether this NEO 2T firmware exposes FE-C, FTMS, or both.
- Whether the write characteristic requires write-without-response or write-with-response.
- Whether notifications must be enabled before every command or only once per connection.
- The exact packet length and field interpretation for the installed firmware.
- Whether the resistance value used by the reference implementation maps to the desired RPM-class experience.
- Whether the trainer must first be claimed, initialized, or placed into a particular FE-C state.
- Which FE-C packet variant is accepted by this firmware: `0x4E`/XOR or `0x4F`/sum.

Use nRF Connect or equivalent BLE inspection software on the physical Android phone to record the services, characteristic properties, and notification traffic before writing the production command path.

## Licensing Caution

Use the external projects as protocol references and reimplement the behavior in this application. Do not copy source code wholesale. Check the applicable licenses before reusing any code or distributing derived work.

## Current App Status

## Hardware Confirmation

The physical Tacx NEO 2T was inspected with nRF Connect on Android. It confirmed the expected Tacx FE-C GATT layout:

| UUID suffix | Confirmed role | Properties observed |
| --- | --- | --- |
| `fec1` | Tacx primary service | Service |
| `fec2` | Trainer-to-app notification characteristic | Notify |
| `fec3` | App-to-trainer write characteristic | Write, Write Without Response |

This confirms the correct BLE transport and removes the need to guess which characteristic should receive notifications or commands. The first notification capture also confirms the `0x4E` packet variant on this trainer.

The notification subscription and diagnostic packet logging are now implemented.

## First Real Notification Capture

The diagnostic build was deployed to the physical Android phone and connected to the NEO 2T. The app received a continuous stream of 13-byte notifications from `fec2`.

Representative packets included:

```text
A4 09 4E 05 19 D5 00 31 01 00 00 20 3A
A4 09 4E 05 10 19 3F 14 00 00 FF 24 1F
A4 09 4E 05 47 FF FF FF 01 00 00 20 7F
A4 09 4E 05 50 FF FF 01 59 00 3B 0B DE
```

This confirms several points on the actual trainer:

- FE-C notifications are active and arrive repeatedly.
- The trainer uses the `A4 09 4E 05` framing seen in SHIFTR.
- The notification payload includes page `0x19` (specific trainer data), page `0x10` (general FE data), and page `0x47` (command status).
- The observed packet length is 13 bytes, matching the FE-C data format described by the reference implementations.
- The trainer is producing status and telemetry data before our app sends any command.

The packet samples should be retained as a baseline. The app now parses page `0x47` command status and the first basic-resistance write has been verified against the trainer.

`TacxNeoService` currently:

- Scans for named BLE devices.
- Connects with `BluetoothGatt`.
- Discovers the FTMS service and Tacx service.
- Prefers the FTMS Control Point and falls back to the Tacx `fec3` characteristic.
- Sends FE-C basic-resistance commands through `fec3` using Write Without Response.
- Enables Tacx `fec2` notifications and logs received packet bytes.

The next high-value change is to expose command status and resistance errors visibly in the UI, then finalize the meaning and range of the preset values.

## SHIFTR-Specific Findings

SHIFTR confirms several details that are directly relevant to this app:

- The author owns and tests with a Tacx NEO 2T.
- The trainer is used as an FE-C-over-BLE device.
- FE-C data from `fec2` is forwarded as notifications and includes pages such as page `16`, page `25`, and page `54`.
- The maximum basic resistance is obtained from the FE-C capabilities response rather than assumed permanently.
- When no client remains connected, SHIFTR sends a default track-resistance command to return the trainer to a normal simulation state.
- Its FTMS emulation supports FTMS target power and indoor-bike simulation parameters by translating them into FE-C target-power and track-resistance commands.

SHIFTR is licensed under GPL-3.0. Its source is valuable for protocol research, but it should not be copied into this application without addressing the license implications. Reimplement the protocol behavior independently.
