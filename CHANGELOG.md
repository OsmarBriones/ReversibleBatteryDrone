# Changelog

## 1.1.0
- Refactored activation lifecycle into an Alternating Activation Mode (Charge -> Off -> Drain -> Off) for 100% Host-Only compatibility without client desynchronization.
- Added smart battery override: automatically forces Drain mode when completely discharged (0% battery) and Charge mode when fully charged (100% battery).
- Unified toggle handling across local inputs and networked `onToggle` events.

## 1.0.0
- First release!
