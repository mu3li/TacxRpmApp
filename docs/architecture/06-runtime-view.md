# 6. Runtime View

<!-- ARC42 §6 — Brief format. Document key runtime scenarios as sequence diagrams. -->

## Key Scenarios

| ID | Scenario | Actors | Trigger |
|----|----------|--------|---------|
| RT-1 | Connect to the trainer | Rider, MainPage, DevicePage, TacxNeoService, NEO 2T | Tap **CONECTAR** |
| RT-2 | Apply a resistance preset | Rider, MainPage, TacxNeoService, NEO 2T | Tap a preset apply button |
| RT-3 | Receive an FE-C notification | NEO 2T, TacxNeoService | Continuous, unsolicited, once subscribed |

---

### RT-1: Connect to the trainer

```mermaid
sequenceDiagram
    participant Rider
    participant MainPage
    participant DevicePage
    participant Svc as TacxNeoService
    participant NEO as NEO 2T

    Rider->>MainPage: Tap CONECTAR
    MainPage->>MainPage: Request Bluetooth permission
    MainPage->>DevicePage: Navigate
    Rider->>DevicePage: Tap PESQUISAR DISPOSITIVOS
    DevicePage->>Svc: ScanDevicesAsync()
    Svc->>NEO: BLE scan (8 s, named devices only)
    NEO-->>Svc: Advertisement
    Svc-->>DevicePage: BleDeviceInfo list
    Rider->>DevicePage: Tap LIGAR
    DevicePage->>Svc: ConnectAsync()
    Svc->>NEO: ConnectGatt (12 s timeout, up to 2 attempts)
    NEO-->>Svc: Connected
    Svc->>NEO: DiscoverServices
    NEO-->>Svc: fec1 / fec2 / fec3 (and possibly FTMS)
    Svc->>NEO: Write CCCD on fec2 (enable notifications)
    Svc-->>DevicePage: Success
    DevicePage-->>MainPage: ConnectionSucceeded
```

Connection is considered successful when either the FTMS Control Point or the Tacx `fec3` write characteristic is present. Failure surfaces as a Portuguese status string, commonly Android GATT status `133`.

---

### RT-2: Apply a resistance preset

```mermaid
sequenceDiagram
    participant Rider
    participant MainPage
    participant Svc as TacxNeoService
    participant NEO as NEO 2T

    Rider->>MainPage: Tap preset apply
    MainPage->>Svc: SetResistanceAsync(value)
    Svc->>Svc: Clamp 0..200, build FE-C 0x30 packet, XOR checksum
    Svc->>NEO: Write Without Response on fec3
    NEO-->>Svc: Notification page 0x47 (command status)
    Svc->>Svc: Decode status
    Svc-->>MainPage: LastConnectionStatus
    MainPage->>MainPage: Colour the active preset
```

`+`/`-` adjust the stored value and re-send only when that preset is the active one.

---

### RT-3: Receive an FE-C notification

```mermaid
sequenceDiagram
    participant NEO as NEO 2T
    participant Svc as TacxNeoService

    NEO-->>Svc: 13-byte packet on fec2
    Svc->>Svc: Log hex
    alt Page 0x36 - capabilities
        Svc->>Svc: Store MaximumResistance
    else Page 0x47 - command status
        Svc->>Svc: Decode success / fail / not supported / rejected
    else Page 0x10 or 0x19 - telemetry
        Svc->>Svc: Discarded
    end
```

Pages `0x10` (general FE data: speed, distance) and `0x19` (specific trainer data: cadence, instantaneous power) arrive continuously and are currently dropped without being decoded.
