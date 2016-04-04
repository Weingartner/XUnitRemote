XUnitRemote
===========

This is a simple but powerful extension to XUnit that allows you to run tests inside remote containers/processes. For your remote process you  
have to write a small extension to declare how to start the process. The process also requires that you add a service running within it to communicate 
with XUnit itself.

There are two sub projects that demonstrate using the system.

	XUnitRemote.Test

is a set of unit tests that run within a remote process. The remote process itself is defined in the
sub projects

	XUnitRemote.Test.SampleProcess

The hooks for XUnit need to be defined like below
