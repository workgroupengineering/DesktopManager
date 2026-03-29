# DesktopManager + EasyControlX + Codex Plan

This plan tracks the end-to-end work needed to make Windows desktop automation feel reliable for:

- local operator use
- remote controller use through EasyControlX
- Codex-driven verification through MCP, skills, and a dedicated plugin

## Status Legend

- `[x]` already exists in a meaningful way
- `[ ]` needed or not complete enough yet
- `[-]` intentionally deferred or optional

## End Goal

Create a smooth, safe, and verifiable Windows automation stack where:

1. `DesktopManager` is the shared desktop automation and evidence engine.
2. `EasyControlX` is the remote session, preview, and transport layer.
3. A Codex plugin exposes a long-lived MCP workflow for inspect -> act -> verify loops.
4. Codex can safely verify real Windows work using screenshots, window/control inspection, targeted input, and artifact capture.

## What We Have Today

### DesktopManager Core

- [x] Shared C# desktop automation library
- [x] PowerShell module
- [x] CLI surface
- [x] MCP server hosted by the CLI
- [x] Window inventory and selection
- [x] Monitor inventory and geometry
- [x] Desktop, monitor, window, and control screenshots
- [x] Window move, snap, minimize, restore, and focus actions
- [x] Window-relative click, drag, and scroll actions
- [x] Named window targets
- [x] Named control targets
- [x] Win32 and UI Automation control inspection
- [x] Control click, text, and key actions
- [x] Whole-window typing and key sending
- [x] Layout save/apply/assert support
- [x] Snapshot save/restore support
- [x] Mutation evidence options on newer MCP flows
- [x] Existing DesktopManager operator skill for Codex

### DesktopManager Reliability and Safety

- [x] Read-only-by-default MCP mode
- [x] Mutation gating with explicit allow flags
- [x] Foreground-input opt-in
- [x] Hosted-session and interactive-session awareness in the ecosystem
- [x] Repo-owned test app for safer UI validation
- [x] Better-than-basic window typing paths with fallback modes
- [x] Geometry-aware window-relative mouse actions

### EasyControlX Today

- [x] Shared contracts package
- [x] Windows agent
- [x] Windows controller app
- [x] iOS controller app
- [x] WebSocket control channel
- [x] Pairing and trusted-controller model
- [x] Window inventory through DesktopManager-backed adapters
- [x] Window actions through DesktopManager-backed adapters
- [x] Monitor inventory through DesktopManager-backed adapters
- [x] Desktop, monitor, and window preview endpoints
- [x] Interactive session bridge for service-to-user-session work
- [x] Basic pointer movement, scroll, button, text, and key commands

### Codex Usage Today

- [x] DesktopManager MCP can already be used for inspect-first desktop work
- [x] DesktopManager operator skill already teaches a safe manual workflow
- [x] Codex can already inspect windows, controls, and screenshots through MCP
- [x] Codex can already perform some real Windows validation when the app is structurally accessible

## What Is Missing

### A. Remote Session Model

- [ ] Define one explicit remote desktop session model shared by DesktopManager and EasyControlX
- [ ] Support session attach modes: desktop, monitor, window, control
- [ ] Include session metadata: bounds, DPI, scale, cursor position, active window, timestamps
- [ ] Support session reconnect and resume
- [ ] Support host-visible "remote session active" state

### B. Live Visual Feedback

- [ ] Add frame streaming, not just one-shot preview endpoints
- [ ] Support changed-frame or throttled-frame delivery for desktop and monitor sessions
- [ ] Support window-scoped live preview
- [ ] Include frame metadata needed for exact coordinate mapping
- [ ] Decide initial codec and transport shape
- [ ] Keep one-shot screenshot endpoints as fallback and for evidence capture

### C. Reliable Remote Input

- [x] Add absolute pointer movement for "click what I see" workflows
- [ ] Keep relative pointer movement for trackpad-style control
- [ ] Add input batching and coalescing for low-latency pointer motion
- [ ] Add pointer smoothing or rate control so high-frequency input does not feel jittery
- [x] Add drag semantics tied to streamed-session coordinates
- [ ] Add richer keyboard model: chords, modifiers, repeat, press/release, navigation groups
- [x] Route window-targeted typing through DesktopManager smart input paths instead of only raw global input
- [ ] Route control-targeted text and key actions through DesktopManager when a control is known

### D. Verification-First Automation

- [ ] Every remote action should be able to return structured evidence
- [x] Capture optional before and after screenshots for important mutations
- [x] Return resolved window/control metadata for targeted actions
- [x] Return elapsed time, success reason, and failure reason
- [x] Add post-action checks for active window, cursor position, or control value when applicable
- [x] Return best-effort observed text snapshots for window-targeted `keyboard.text` acknowledgements
- [x] Return focused-control identity in window-targeted input acknowledgements
- [x] Return focused-control identity in `/actions/window` evidence
- [x] Add session artifact bundles for Codex runs

### E. Coordinate System and Mapping

- [x] Normalize logical vs physical pixels for remote sessions
- [x] Map preview-space coordinates to desktop-space coordinates
- [ ] Map preview-space coordinates to window client-area coordinates
- [ ] Handle multi-monitor offsets and mixed DPI cleanly
- [ ] Expose coordinate conversion helpers from shared DesktopManager code

### F. DesktopManager Product Work

- [ ] Add a first-class remote session service in DesktopManager
- [ ] Expose stream-friendly capture backends
- [ ] Expose cursor position observation
- [x] Expose active-window and focused-control observation APIs suitable for remote verification
- [ ] Add stronger public APIs for remote-safe pointer and keyboard operations
- [ ] Add public contracts for evidence and remote-session artifacts
- [ ] Expand tests for real mouse movement, remote typing, and coordinate mapping

### G. EasyControlX Product Work

- [x] Add separate session/control contracts for streamed remote work
- [x] Add a frame channel alongside the command channel
- [x] Move from one-command-one-ack behavior for pointer motion to batched or non-blocking flow
- [x] Add attach-to-window and attach-to-monitor flows in the host
- [x] Add a first Windows controller live window session flow with streamed preview, click, and text send
- [x] Surface live session acknowledgement evidence in the Windows controller UI
- [ ] Add richer controller UX for remote window selection and live preview
- [ ] Add per-capability policy distinctions for view vs input vs control-text access
- [ ] Add host-side session prompts and visibility for sensitive access
- [x] Expose active remote-session status through host diagnostics and controller summaries
- [x] Show a host-side remote-session indicator in the interactive desktop session
- [x] Distinguish host-side viewing-only versus input-active session state
- [x] Distinguish host-side view-only versus input-enabled session permissions
- [x] Add controller-side editing for `session.view`, `session.input`, and `window.input`
- [x] Add controller-side editing for host-default `session.view`, `session.input`, and `window.input` exposure
- [x] Show host baseline versus selected-controller effective remote-session access in the controller UI
- [x] Show live session mode as `view only`, `input allowed`, or `input active` in the controller preview flow
- [x] Show whether live session mode is host-reported or controller-inferred in the controller preview flow
- [x] Refresh host diagnostics opportunistically during live sessions so preview mode upgrades from inferred to host-reported
- [x] Back off live-session status polling once host-reported session diagnostics are flowing steadily
- [x] Show live-session mode freshness in the controller so operators can judge how recent host-reported state is
- [x] Show live-session sync cadence in the controller so operators understand fast-sync versus steady-sync behavior
- [x] Show a live-session host-status heartbeat so operators can spot healthy versus stale diagnostics
- [x] Show action hints when the live-session heartbeat is waiting or stale
- [x] Add a one-click live-status refresh action for recovery without a full controller refresh
- [x] Add a one-click live-preview refresh action for recovery without restarting the session
- [x] Show the last manual live-session recovery result in the controller UI
- [x] Distinguish manual recovery results from automatic live-session sync updates in the controller UI
- [x] Highlight live-session update failures visually with a severity badge and tinted status card
- [x] Surface live-session update severity on the preview overlay itself
- [x] Make live-session severity badges identify whether manual recovery or background sync is the issue
- [x] Show a short next-step action cue on the preview overlay for live-session failure states
- [x] Add clickable preview-overlay recovery buttons for live-session status and preview refresh

### H. Codex Plugin and Skills

- [ ] Create a dedicated local Codex plugin for Windows verification workflows
- [ ] Add one MCP server definition that connects to the local or remote Windows host
- [ ] Add a primary skill for inspect -> act -> verify desktop workflows
- [ ] Add a skill for app smoke tests on Windows
- [ ] Add a skill for visual verification and artifact capture
- [ ] Add bootstrap scripts for host health checks, session startup, and artifact collection
- [ ] Make long-lived MCP sessions the default so UI Automation caches and state stay warm

### I. Test Harness and Validation

- [x] Expand the repo-owned test app with remote-control scenarios
- [x] Add scenarios for live typing into real edit fields
- [ ] Add scenarios for Chromium-style editable surfaces
- [x] Add scenarios for drag and drop
- [ ] Add scenarios for canvas or coordinate-driven interaction
- [ ] Add end-to-end tests that prove "what was seen" matches "what was clicked"
- [x] Add end-to-end tests for service-hosted remote sessions, not only in-process calls

### J. Security and Policy

- [ ] Split capabilities more precisely:
- [ ] `desktop.view`
- [ ] `window.view`
- [ ] `desktop.input`
- [ ] `window.input`
- [ ] `control.text`
- [ ] `control.keys`
- [ ] Add stronger per-controller access policy management
- [ ] Add audit logging for session start, stop, attach target, and sensitive actions
- [x] Add audit logging for remote session start, stop, and attach target
- [ ] Add policy defaults for local-only vs LAN access
- [ ] Add safer defaults for foreground-stealing behavior

### K. Documentation and Operations

- [ ] Document the end-to-end architecture for DesktopManager + EasyControlX + Codex
- [ ] Document the intended division of responsibility between the repos
- [ ] Document the remote session lifecycle
- [ ] Document supported verification levels: structural, visual, interactive
- [ ] Document safe operator guidance for live desktop sessions
- [ ] Document how to install and use the Codex plugin locally

## Suggested Delivery Phases

### Phase 0: Align the Architecture

- [ ] Decide the division of responsibility:
- [ ] DesktopManager owns automation, coordinates, evidence, and capture primitives
- [ ] EasyControlX owns transport, pairing, controller UX, and remote session lifecycle
- [ ] Codex plugin owns MCP packaging, skills, and workflow guidance
- [ ] Freeze the first session contract shape
- [ ] Freeze the first capability model for remote view/input/control actions

### Phase 1: Make Remote Validation Practical

- [ ] Add a DesktopManager remote session abstraction
- [x] Add absolute coordinate mapping for window and monitor sessions
- [ ] Add richer evidence objects for actions
- [x] Add EasyControlX attach-to-window session flow
- [x] Add throttled live preview stream for a selected window or monitor
- [ ] Add Codex plugin skeleton with one verification skill

### Phase 2: Make It Feel Smooth

- [x] Add batched pointer events
- [ ] Add non-blocking pointer move handling
- [x] Add drag-aware streaming sessions
- [x] Add cursor and active-window telemetry in the session stream
- [x] Show live input verification evidence in the Windows controller during a session
- [x] Show live frame telemetry over the Windows controller preview
- [ ] Improve controller UX around live interaction

### Phase 3: Make It Trustworthy

- [ ] Add strong end-to-end harness tests
- [ ] Add artifact bundles and replayable evidence
- [ ] Add detailed audit logging and controller policy controls
- [ ] Add docs and runbooks for local and LAN usage

## Definition of Done

The stack is "smooth butter" for Codex when all of the following are true:

- [ ] Codex can start one long-lived MCP session against a Windows host
- [ ] Codex can inspect windows, controls, and live frames in the same session
- [ ] Codex can choose between absolute visual clicking and targeted control interaction
- [ ] Codex can type into a selected window or control reliably
- [ ] Codex can verify the result with structure plus screenshots
- [ ] Codex can collect artifacts automatically when a workflow matters
- [ ] Codex can run safely with clear host visibility, policies, and audit logs
- [ ] The main flows are covered by repo-owned end-to-end tests

## Immediate Next Recommended Steps

- [ ] Agree the repo boundary and ownership model
- [x] Design the first remote session contract
- [ ] Add DesktopManager coordinate-mapping helpers for remote sessions
- [x] Add EasyControlX live window-session preview stream
- [ ] Add a minimal Codex plugin skeleton with one MCP server and one skill
- [ ] Build one golden-path end-to-end scenario:
- [x] launch or attach to a test app
- [x] stream the window
- [x] click a known field
- [x] type text
- [x] verify the text
- [x] capture before and after artifacts
