
XSLT Mediator 2011 v1.3
----------------------------------------
----------------------------------------

Release Notes:
------------------

 - Added the positioning variables (by Chris Summers
 - Added check against duplicate items in the argument list passed to the transformation to avoid exceptions
 - Added extension object initialization for helpers that use constructors accepting Engine and Package instances



V 1.1:
-------

  - Changed the AddExtensionObjects method to a virtual method to allow inheriting classes to easily add additional extension      objects to the transformation.

  - Added logic to load extension objects from an assembly stored in Tridion as a TBB. to Enable this the                      tridion.ContentManager.config file should be changed in the following way: 

     <mediator matchMIMEType="text/xml" type="Tridion.Extensions.Mediators.XsltMediator" assemblyPath="D:\Tridion\bin\Tridion.Extensions.Mediators.dll" >

          <parameters>					
		<parameter name="loadFromTridion" value="true"/>
		<parameter name="interfaceName" value="IXSLTHelper"/>
		<parameter name="templateID" value="tcm:2-123-2048"/>
	  </parameters>
     </mediator>

     The interface name parameter should have the name of the interface you intend to use as a way to distiniguish the classes      which will be added to the XSLT transformation as extension objects, these classes should also be decorated with an            attribute which is provided in the base project (also available on SDLTridionWorld.com).

     The templateID parameter should contain the TCM URI of the template building block which stores a .NET assembly in             Tridion, commonly this would be the same assembly containing the templates.

     There are two types of supported constructors for extension objects, a default (parameterless) constructor and a      constructor which accepts an instantiated Engine object.

Installation notes:
-------------------
To enable the Mediator the Tridion CM configuration requires a small modification:

The file in the folder [InstallationFolder]\Config is called Tridion.ContentManager.config

Preferably create a backup of this file and then open it in your preferred text editor and navigate to the <tridion.templating> element, then the <mediators> elment and add the following line:

<mediator matchMIMEType="text/xml" type="Tridion.Extensions.Mediators.XsltMediator" assemblyPath="D:\Tridion\bin\Tridion.Extensions.Mediators.dll" />

If you changed the namespace or assembly name or path make sure that change is reflected in this configuration statement.


To start using the registered Mediator restart the Tridion Content Manager COM+ application as well as the Publisher service and IIS. 



Caution:
----------
This code is supplied as is.
It has been tested quite thoroughly but there is no guarantee it will perform similarly under all circumstances and on all environments.
It is strongly recommended that the Mediator should be tested extensively before applied on a Production environment.


Created by:
------------
Frank Van Puffelen
Chris Summers
Yoav Niran 

== SDLTridion ==