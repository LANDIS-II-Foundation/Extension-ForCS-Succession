rem Batch File to Run a Scenario 
rem The 'rem' keyword indicates that this is a remark

call landis-ii-8 scenario.txt

rem Always use the 'call' keyword before invoking landis-ii in a batch file.
rem The call keyword is necessary because the landis-ii command is itself a batch file.

pause

rem Add a pause so that you can assess whether the scenario ran to completion or whether 
rem it encountered input parameter errors or any other error.

