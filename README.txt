------------------------------------------------------
Plywood Distributed Software Version Management System
------------------------------------------------------

Introduction
------------

Plywood is a distributed software version management and deployment system. The system is built around the concept of a restful object store, initially Amazon's Simple Storage Service. This central store can be any service which allows the storage of files organised in folders. The assumption is made that this service has a high latency, therefore reducing round-trips is important. 

Licence and Credits
-------------------

This project has been released under the Apache 2.0 licence (see LICENCE-2.0.txt for details).

I would also like to thank the Adfero Ltd, London. For allowing me the time to create my crazy ideas and giving their permission to open source this project, not to mention the always useful feedback on bugs and new features!

Project structure
-----------------

The entire solution is composed of several interrelated projects:

Core:
The core defines all of the libraries used to communicate and interact with the central storage system.

Functional testing:
A project which simply tests that all core functions operate as expected.

Unit tests:
Tests isolated pieces of logic such as serialisation or validation methods.

Pull service:
A windows service that can be installed on a server instance to pull updates from a specific target.

IIS Deployment:
A module to provide tightly integration deployment functionality with the IIS web server software.

Command line:
Command line tools to manually interact with the plywood system.

Sawmill:
Web-based management tool for Plywood.

Rational
--------

The rational behind creating this system is to create a pull-based deployment system with as few dependencies as possible. The benefit of a pull based system is to make distribution to groups of servers easier as no extra central configuration is required to set up a new instance. 

Second to this is the fact that security is much easier to control where servers only require outgoing connections (and the use of http(s) also helps the simplicity of the security setup).


-------------
Core Concepts
-------------

Example Plywood Setup Structure
-------------------------------

Contexts
|- Company.ProductA
  |- Packages
   |- Web App 1
    |- Versions
     |- 1.0.0 GA Release
     |- 1.0.1 Alpha Release
  |- Roles
   |- Staging Server
    |- Apps
     |- Web App 1 : 1.0.1
    |- Servers
     |- Test Instance
      |- Update Logs
   |- Production Server
    |- Apps
    ||- Web App 1 : 1.0.0
    |-Servers
     |- Zone A Instance
      |- Update Logs
     |- Zone B Instance
      |- Update Logs

Contexts
------
Contexts are nothing more than a high-level divide within the system to separate applications, targets and the versions. Arguably these are not so vital as multiple instances could also be set up. However, that would then introduce the need to manage more instances of the system which is an undesirable maintenance requirement.

Packages
----
Packages are the individual units which can be deployed. A package could be something such as a single web application, service or other deployable unit.

Versions
--------
Versions sit within apps and represent a single deployable state of a single application at a single point of time. Hence, versions have timestamps by which they are naturally ordered.

Roles
-------
A role at its fundamental level is the set of packages to be deployed to a set of machines. Roles are used to represent one or many physical machines and are therefore used to specify the applications (and versions) that should be deployed on to each machine in that target group.

Tags
----
All of the above entities in the system have a tags collection which is simply a set of key value pairs to store custom metadata about the entity. Keys can only be a single line string and cannot conflict with names of any of the properties on the entity (e.g. "Name" or "Key"). Values have no such
limitations and can contain multiple lines of text content.

Hooks
-----
Hooks are simply tags with specific names which can be used to instruct the system to perform actions on the occurrence of events within the system.

Available hooks are listed below. Use the hook name as the tag name and set the value of the tag to the command to be run.

hook-install
Can be applied to apps and versions. Runs when an app is about to be installed. 

hook-installed
Can be applied to apps and versions. Runs when an app has successfully been
installed.

hook-update
Can be applied to apps and versions. Runs when a version of an app is about
to be updated. The apps hook will be run before the versions hook.

hook-updated
Can be applied to apps and versions. Runs when a version of an app has
successfully been updated. The versions hook will be run before the app
hook.

hook-uninstall
Can be applied to apps. Runs when an app is about to be uninstalled. 

hook-uninstalled
Can be applied to apps. Runs when an app has successfully been uninstalled.