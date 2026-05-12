# CortriumBLE

This is meant as a very basic starting point for working with the Cortrium device in academic courses.

If features a BLE connection to the C3+ device from Cortrium A/S, Denmark and a basic ECG graph.

I have also added VERY basic R-peak detect which can be used for RR-interval, CSI and ModCSI calculation.

In the future, more advanced R-peak detect, RR-interval, and CSI and ModCSI handling will be added. 

Please note, that the signal processing might be better performed using Python libraries, due to availability and speed.

The source code is deliberately crammed into a as few files as possible. I would encourage anyone planning on using this to creae sepatarte pages for connection, settings, graphs, etc. 

# telemonitoring_project

Modified the existing code to
1. Put the ECG data in a AWS database
2. Gather the accelerometer data from the phone and also sends it to the database

This allows for a hospital computer to fetch the data and do more advanced analysis on it.

For now we have decided to keep the graph drawing feature as its a good indicator if the program is crashing.
