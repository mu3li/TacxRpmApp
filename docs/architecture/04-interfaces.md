# 4. Interface Documentation

<!-- CRITICAL: All APIs to other systems MUST be documented here. This is the authoritative interface inventory. -->
<!-- This template supports auto-generation from OpenAPI, AsyncAPI, and WSDL specs. -->

## Interface Inventory

<!-- Every interface the system exposes or consumes. Keep this table current. -->

| Name | Source | Target | Protocol | Data Exchanged | Direction | Auth | Criticality | Docs Link |
|------|--------|--------|----------|----------------|-----------|------|-------------|-----------|
| Tacx FE-C control | TacxRpmApp | Tacx NEO 2T | ANT+ FE-C over BLE GATT | Basic-resistance command `0x30` | Outbound | None (BLE pairing only) | Critical | `docs/copilot-protocol-findings.md` |
| Tacx FE-C telemetry | Tacx NEO 2T | TacxRpmApp | ANT+ FE-C over BLE GATT notifications | Pages `0x10`, `0x19`, `0x36`, `0x47`, `0x50` | Inbound | None | Critical | `docs/copilot-protocol-findings.md` |
| Heart Rate Measurement | BLE heart-rate strap | TacxRpmApp | BLE Heart Rate profile | Heart rate in BPM | Inbound | None | Medium | Bluetooth SIG HRS 1.0 |
| FTMS Control Point | TacxRpmApp | Tacx NEO 2T | BLE GATT | *(none — discovered but never written)* | Unused | None | Low | Bluetooth SIG FTMS 1.0 |

### Criticality Levels
- **Critical** — Failure causes system outage or data loss
- **High** — Degraded experience, manual workaround exists
- **Medium** — Feature unavailable, core flow unaffected
- **Low** — Convenience feature, no business impact

---

## Interface Details

### 📡 Interface: Tacx FE-C over BLE

| Field | Value |
|-------|-------|
| **Source** | TacxRpmApp (BLE central) |
| **Target** | Tacx NEO 2T (BLE peripheral) |
| **Protocol** | ANT+ FE-C messages tunnelled through a proprietary Tacx GATT service |
| **Pattern** | Fire-and-forget write out; unsolicited notification stream in |
| **Service / Characteristics** | Service `6e40fec1-b5a3-f393-e0a9-e50e24dcca9e`; notify `…fec2`; write `…fec3`; CCCD `0x2902` |
| **Auth / Security** | None. BLE link-layer only. First-come-first-served — no pairing key, no application-level authentication. |
| **Versioning** | None. Behaviour is firmware-dependent and verified empirically against one NEO 2T. |

**Characteristic roles** — named from the client's perspective:

| Characteristic | Properties observed | Role |
|---|---|---|
| `…fec2` | Notify | Trainer → app |
| `…fec3` | Write, Write Without Response | App → trainer |

**Outbound message — basic resistance (`0x30`):**

```text
A4 09 4E 05 30 FF FF FF FF FF FF <resistance> <checksum>
```

| Field | Value |
|---|---|
| Framing | `A4 09 4E 05` — ANT sync, length, message ID `0x4E` (broadcast data), channel `0x05` |
| Page | `0x30` — basic resistance |
| Padding | Six `0xFF` bytes |
| `<resistance>` | `0`–`200`, clamped by the app. Documented by references as 0.5 % increments. |
| `<checksum>` | XOR of all preceding bytes |
| Write type | Write Without Response |

Message ID `0x4E` and XOR checksum were confirmed against the physical trainer. An alternative variant (`0x4F` with an additive checksum) appears in other references and is **not** what this NEO 2T uses.

**Inbound messages** — 13-byte notifications on `…fec2`:

| Page | Content | Parsed today |
|---|---|---|
| `0x10` | General FE data — speed, accumulated distance, elapsed time | **No** |
| `0x19` | Specific trainer data — cadence, instantaneous power, accumulated power | **No** |
| `0x36` | Capabilities — maximum resistance (bytes 9–10, little-endian) | Yes |
| `0x47` | Command status — echoed command and status byte | Yes |
| `0x50` | Manufacturer identification | No |

Representative captures are retained in `docs/copilot-protocol-findings.md`.

**Error Handling:**

| Status / Error Code | Meaning | Consumer Action |
|--------------------|---------|-----------------|
| GATT `133` | Generic Android connection failure | One automatic retry after 700 ms, then report to the user |
| Connection timeout (12 s) | Peripheral did not respond | Retry once, then report |
| `SecurityException` | Bluetooth permission not granted | Surface a permission message |
| No FTMS **and** no `fec3` | Not a supported trainer | Report the discovered service list |
| Page `0x47` status `0x00` | success | Treat command as applied |
| Page `0x47` status `0x01` / `0x02` / `0x03` / `0xFF` | fail / not supported / rejected / uninitialized | Surface the status; the command did not take effect |

**SLA / Non-Functional:**

| Metric | Target |
|--------|--------|
| Command latency | Imperceptible while riding (no measured target) |
| Notification rate | Continuous once subscribed; exact rate not measured |
| Session duration | Must hold for 60 minutes continuously |
| Availability | Best-effort; a dropped link must be recoverable without data loss |

---

### 📡 Interface: BLE Heart Rate profile

| Field | Value |
|-------|-------|
| **Source** | Heart-rate strap (BLE peripheral) |
| **Target** | TacxRpmApp (BLE central) |
| **Protocol** | Bluetooth SIG Heart Rate Service |
| **Pattern** | Notification stream |
| **Service / Characteristics** | Service `0x180D`; measurement `0x2A37`; CCCD `0x2902` |
| **Auth / Security** | None |
| **Versioning** | Standard profile; no versioning |

**Message decoding:** flags bit 0 selects an 8-bit or 16-bit little-endian heart-rate value. Values outside `1`–`300` are rejected as implausible.

**Error Handling:**

| Status / Error Code | Meaning | Consumer Action |
|--------------------|---------|-----------------|
| Service `0x180D` absent | Not a heart-rate device | Report and abort connection |
| CCCD write fails | Notifications cannot be enabled | Report and abort connection |
| Connection timeout (12 s) | Strap unreachable or asleep | Report |

**Status:** implemented in `HeartRateService` but **not reachable from the application** — no DI registration, no UI.

---

## Integration Patterns Reference

- [x] **BLE GATT (central role)** — Notification streams inbound, Write Without Response outbound
- [ ] **Synchronous REST** — Request/response over HTTPS
- [ ] **Synchronous gRPC** — Binary protocol, service mesh
- [ ] **Async Messaging (AMQP)** — RabbitMQ, point-to-point or fanout
- [ ] **Event Streaming (Kafka)** — Partitioned topics, consumer groups
- [ ] **Batch / File Transfer** — SFTP, S3, scheduled jobs
- [ ] **GraphQL** — Query-based API
- [ ] **WebSocket** — Bidirectional real-time

## Auto-Generation Guide

No machine-readable interface specifications exist for this project and none are expected. The FE-C interface is a proprietary binary protocol documented empirically in `docs/copilot-protocol-findings.md`; the Heart Rate profile is a published Bluetooth SIG specification. This section is therefore intentionally not applicable — keep the packet captures current instead.
