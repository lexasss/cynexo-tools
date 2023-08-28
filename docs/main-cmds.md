# Cynexo main commands

## General

- setVerbose 0 | 1
  --- controls serial port echo
- setCAChannel 1..13
  --- Sets the channel to be used as the Clean Air channel
- disableAllValves
  --- Closes all solenoid valves
- enableAllValves (solenoid valves only)
  --- Opens all solenoid valves

## Calibration

- setFlow [ID:SLPM;]*
  --- Automatically calibrates the flow of the given channels
- stopCalibration
  --- Interrupts the calibration process if needed
  
## Channel operation

- setChannel 1..13
  --- Set the active channel you wish to operate on
- setValve 0|1
  --- Set the state of the valve of the active channel
- readFlow
  --- Read the value of the air flow rate for the currently active channel.
- setDirection 0|1
  --- Set the direction of the stepper valve
- steps N
  --- Moves the valve stepper motor by the indicated number of steps
  !WARNING: use N=5-10 in tests, unless it is clear that larger N is safe!
- openValveTimed MS
  --- Opens the valve of the active channel for a precise amount of time
- CfOffOpenValveTimed MS
  --- Opens the valve of the active channel for a precise amount of time and at the same time disables the constant flow channel. As soon as the delivery of the odour ends the constant flow channel is reactivated. We recommend the use of a clean air channel (i.e. a standard odour channel used without adding any odour) in addition to rather than purely instead of the constant flow channel.
- CaOffOpenValveTimed
  ---   Opens the valve of the active odour channel for a precise amount of time and at the same time disables the clean air channel. As soon as the delivery of the odour ends the clean air channel is reactivated.

## Triggered channel operation

- Tb_in_breathSound 0..13 durationMS delayMS
 --- Waits for the trigger IN from Spir-0, indicating that the subject has started **exhaling**, opens the valve of the selected channel for the selected amount of time, and  generates the trigger necessary for the Spir-0 audio module to generate the sound after the selected delay. 
- Tb_out_breathSound 0..13 durationMS delayMS
 --- Waits for the trigger IN from Spir-0, indicating that the subject has started **inhaling**, opens the valve of the selected channel for the selected amount of time, and  generates the trigger necessary for the Spir-0 audio module to generate the sound after the selected delay. 