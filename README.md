## A C# library for the Siglent SPD3303X Series Bench Supply,it does not utilize the Ni Visa Driver/API 

# Commands availble on this Library

| DataType | Command                  | Description                                                               | Expected return                      | Getter/Setter |
|----------|--------------------------|---------------------------------------------------------------------------|--------------------------------------|---------------|
|   Void   | public void connect()    | `Connection initialzation`, It initiates the connection to the instrument | N/A                                  |               |
| Void     | public void disconnect() | `Socket termination`, Terminates the connection and cleans the memory     | N/A                                  |               |
| String   | public string getIDN()   | Gets the `*idn?` from the instrument.                                     | BrandName,modelNumber,Serial,version | Getter        |
|CHANNELS| public CHANNELS getActiveChannel()|Gets the Active the channel |a euum with the active channel (ch1 or 2)|Getter|
|double|public double getVoltage(CHANNELS channel)|Gets the Voltage SetPoint of the given channel|00.00|Getter|
|double|public double getCurrent(CHANNELS channel)|Gets the Current SetPoint of the given channel|00.00|Getter|
|double|public double getOutputCurrent(CHANNELS channel)|Gets the current output of givven channel|00.00|Getter|
|double|public double getOutputPower(CHANNELS channel)|Gets the Power reading from the ADC|00.00|Getter|
|CHANNEL_MODE|










* Command Set for Siglent`s SPD3303X-E as listed on the user manual 

The following commands is what its implemented on the library.

# Getters

| Command | Expected return |Notes| DataType | Type |Unit
|---|---|---|---|---|---|
| `*IDN?` | {Vendor},{ModelNumber},{SerialNumber},{Firmware Version},{Hardware Revision}|theMostUniversalCommand | String | Getter |N/D|
| `INSTrument?` | CHx |Gets The Active Channel| String | Getter |N/D|
| `MEASure:CURRent? CHx` | 0.00 |ADC reading| Float | Getter | A |
| `MEASure:VOLTage? CHx` | 0.00 |ADC reading| Float | Getter |V |
| `MEASure:POWEr? CHx` | 0.00 |ADC reading or instrument calc| Float | Getter |W|
| `CHx:CURRent?` | 0.00 |Set value| Float | Getter |A|
| `CHx:VOLTage?` |0.00 |Set value| Float | Getter |V|
|`SYSTem:STATus?`|0xSOMETHING|see "SystemStatus" Table for details|HEX|Getter|N/D

# Setters

| Command | Expected return |Notes| DataType | Type |Unit
|---|---|---|---|---|---|
| `INSTrument CHx` | n/a | - |-| Setter |N/D|
|`CHx:CURRent {VAL}`|n/a|sets the CC|Float|Setter|A|
|`CHx:VOLTage {VAL}`|n/a|sets the output Voltage|Float|Setter|V|
|`OUTPut CHx,{ON/OFF}`|n/a|Turn on/off the specified channel|-|Setter|N/D|
|`OUTPut:TRACK {0/1/2}`|n/a|Sets the channel mode on Indie, Serial or Paralel|-|Setter|N/D|
|`OUTPut:WAVE CHx,{ON/OFF}`|n/a| Turn on/off the Waveform Display function of specified channel.|-|Setter|N/D|
|`*SAV {1/2/3/4/5}`|n/a|Save current state in nonvolatile memory|-|Setter|N/D|
|`*RCL {1/2/3/4/5}`|n/a|Recall state that had been saved from nonvolatile memory.|-|Setter|N/D|

* IP setup

# Getters

|Command|Expected return|Description|Type|
|---|---|---|---|
|`DHCP?`|DHCP:ON|Query whether the automatic network parameters configuration function is turn on|Getter|
|`IPaddr?`|192.168.0.106|Query the current IP address of the instrument|Getter|
|`MASKaddr?`|255.255.255.0|Query the current subnet mask of the instrument|Getter|
|`GATEaddr?`|192.168.0.1|Query the current gateway of the instrument|Getter|


# Setters
|Command|Expected return|Description|Type|
|---|---|---|---|
|`DHCP {ON/OFF}`|n/a|Assign the network parameters (such as the IP address)for the instrument automatically.|Setter|
|`IPaddr {IP address}`|n/a|Assign a static Internet Protocol (IP) address for the instrument Note,Invalid when DHCP is on|Setter|
|`MASKaddr {NetMasK}`|n/a|Assign a subnet mask for the instrument Note,Invalid when DHCP is on|Setter|
|`GATEaddr {GateWay}`|n/a|Assign a gateway for the instrument Note,Invalid when DHCP is on|Setter|

# Using the `SYSTem:STATus?` Command
> THis command sums up the current status of the instrument , in to a hex that you have to proccses based on this : 


The return info is hexadecimal format, but the actual
state is binary, so you must change the return info into a
binary. The state correspondence relationship is as
follow

| Bit No. | Corresponding State |
|---|---|
| 0 | 0: CH1 CV mode; 1:CC Mode |
| 1 | 0: CH2 CV mode; 1: CH2 CC mode |  
|2,3  |  01: Independent mode; 10: Parallel mode 11:Series Mode (fromMemory)|
|4|0: CH1 OFF 1: CH1 ON|
|5|0: CH2 OFF 1: CH2 ON|
|6|0: TIMER1 OFF 1: TIMER1 ON|
|7|0: TIMER2 OFF 1: TIMER2 ON|
|8|0: CH1 digital display; 1: CH1 waveform diplay|
|9|0: CH2 digital display; 1: CH2 waveform diplay|

example: 
* Base (CV/CV,Indie mode, CH1 off, CH2 OFF Timers off CH1/CH2 Digital Display) : `0x4`
* CH1ON: `0x14`
* CH2ON: `0x24`
* Serial: `0xc`

## 
The Siglent name is a registered trademark of SIGLENT TECHNOLOGIES CO., LTD, in the United States and/or other countries. 
All other trademarks referenced in the Software or Documentation are the property of their respective owners.

This is an unofficial implementation. Usage of the Siglent name does not confer in any way, implied or otherwise,  
endorsement by or association with SIGLENT TECHNOLOGIES CO., LTD. 