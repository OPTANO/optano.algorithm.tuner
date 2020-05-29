# Typical issues

Be aware of these traps when using *OPTANO Algorithm Tuner*.

## File not found exception
After installing the _OPTANO Algorithm Tuner_ package from nuGet, you may need to alter some project settings: [Fixing FileNotFoundExceptions](typical_issues/missing_reference.md)

## Bad image format exception
This is about x86 and x64 bit code: [Fixing BadImageFormatExceptions](typical_issues/bad_image.md)

## Problems with Distributed Execution of *OPTANO Algorithm Tuner*
If you use *OPTANO Algorithm Tuner* in distributed fashion and neither master nor worker are evaluating anymore, it is possible that they lost connection to each other, but the automatic detection of that failed. Restarting the worker only will usually solve the problem and at most lose the evaluation data of one mini tournament.

If the worker does not connect to the master in the first place, try to explicitely set the IP adress of the master as host name with `--ownHostName=[IPADRESS]` by starting the master.