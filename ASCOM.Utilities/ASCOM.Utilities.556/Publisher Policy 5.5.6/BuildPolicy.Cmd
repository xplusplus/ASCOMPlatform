@Echo off
Call "%VS90COMNTOOLS%vsvars32"
al /link:UtilitiesPolicy.xml /out:policy.5.5.ASCOM.Utilities.dll /keyfile:..\..\..\ASCOM.snk  /version:5.5.6.0 /fileversion:5.5.6.0 /company:"ASCOM Initiative" /product:"ASCOM.Utilities for ASCOM Platform 6"
