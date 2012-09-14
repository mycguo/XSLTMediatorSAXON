using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using Tridion.ContentManager;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Configuration;
using Saxon.Api;
using net.sf.saxon.om;
using log4net;

/*
 * http://www.xmlplease.com/saxonaspnet
 * */


namespace Tridion.Extensions.Mediators
{
    /// <summary>
    /// Using SAXON (XSLT2 processor) as XSLT Mediator 
    /// Allowing the use of XSLT TBBS as part of compound templates
    /// </summary>
    /// <author>Created by Charles Guo, based on XSLT Mediator created by Chris Summers, Frank van Pufflen and Yoav Niran (SDL Tridion)</author>
    /// <version> 1.4 04/16/2012
    ///     Charles Guo:      Using XSLT2 processor SAXON 9.1.0.8 
    /// 
    /// </version>
    /// <version> 1.3 - 02/01/2011
    ///		Chris Summers: Added Ordinal position and regionl posiion support
    ///		Yoav Niran: 
    ///					Minor overall code improvement and enhanced efficiency
    ///					Added check for duplicate keys in package for arguments list
    ///					Refactored ordinal and regional position usage to seperate method
    ///					Added extension object initialization for helpers that use constructors accepting Engine and Package instances
    /// </version>
    /// <version> 1.1b - 09/07/2009
    ///		Yoav Niran: Removed the caching of the assembly as this seems to be causing issues when publishing
    /// </version>
    /// <version>1.1 - 09/06/2009</version>
    /// <version>1.0 - 13/03/2009</version>	
    public class SaxonMediator : IMediator
    {
        #region private Members

        private string m_InputItemName; //name of item in the package to use as the source of the transformation
        private string m_OutputItemName; //name of item in the package to update with the transformation result
        private bool m_GetFromPackage; //whether to use package items as parameters
        private string m_ParPrefix; //prefix for package items to use as xslt arguments
        private string m_outputType; //the output format type to use for the custom package item

        private Tridion.ContentManager.Templating.Item m_XmlInput = null;
        private Package m_Package = null;
        private Engine m_Engine = null;
        private ILog m_Logger = null;

        private string m_HelperInterface = null;
        private string m_HelpersAssemblyTBB = null;
        #endregion

        #region Constants

        private const string ORDINAL_POSITION = "ORDINAL_POSITION";
        private const string REGIONAL_POSITION = "REGIONAL_POSITION";
        private const string OUTPUT_BASE_URI = "BaseTCMURI";

        private const string CONF_LOAD = "loadFromTridion";
        private const string CONF_INTERFACE = "interfaceName";
        private const string CONF_TBB = "templateID";

        private const string HELPER_NS = "TridionXSLTHelperNSAttribute";
        #endregion

        #region Properties

        protected Engine Engine { get { return m_Engine; } }
        protected ILog Logger { get { return m_Logger; } }

        #endregion

        public SaxonMediator()
        {
            m_Logger = LogManager.GetLogger(this.GetType());
            m_Logger.Info("Starging SAXON Mediator");
        }

        #region Mediator Methods

        public void Configure(MediatorElement configuration)
        {
            Dictionary<string, string> pars = new Dictionary<string, string>();

            foreach (ParameterElement pe in configuration.ParameterCollection)
            {
                pars.Add(pe.Name, pe.Value);
            }

            if (pars.Count > 0)
            {
                if (pars[CONF_LOAD].ToLower() == Boolean.TrueString.ToLower())
                {
                    Logger.Info("XSLT Mediator is configured to load xslt helpers from assembly stored in Tridion");

                    m_HelperInterface = pars[CONF_INTERFACE];
                    m_HelpersAssemblyTBB = pars[CONF_TBB];

                    Logger.Debug(String.Format("XSLT Mediator will look for classes implementing '{0}' in assembly TBB: '{1}'", m_HelperInterface, m_HelpersAssemblyTBB));
                }
                else
                    Logger.Debug("XSLT Mediator is not configured to use helpers stored in Tridion");
            }
        }

        public void Transform(Engine engine, Template template, Package package)
        {
            m_Engine = engine;
            m_Package = package;

            m_Logger.Debug(String.Format("XSLT Meditator executing: '{0}' ", template.Id));

            ReadParameters();

            using (MemoryStream templateStream = GetStringAsStream(template.Content))
            {
                var xslt = new XPathDocument(templateStream);// Create an new XmlDocument to hold the XSLT of the current Template Building Block // Load the contents (xslt) of the TBB into the XmlDocument			
                var input = new XmlTextReader(new StringReader(m_XmlInput.GetAsString())); //need to use the stringreader for xml in the package marked with encoding=utf-16 otherwise an encoding exception is thrown during the transformation

                //no need to worry about XsltSettings, the default should work for SAXON
                // var xsltSettings = new XsltSettings(); // Create an XsltSettings object to override the default XSLT settings for XslCompiledTransform            			
                //xsltSettings.EnableDocumentFunction = true; // Enable script and document() funtion (default in .NET is disabled
                //xsltSettings.EnableScript = true;

                var resolver = new TcmUriResolver(engine); //Create a new UriResolver which supports TridionURIs (Only http:// and file:// are supported in the default one)

                //Run Saxon s9 api
                // Create a Processor instance.
                Processor processor = new Processor();

                XsltCompiler compiler = processor.NewXsltCompiler();

                //set resolver
                compiler.XmlResolver = resolver;

                //compile the xslt
                XsltTransformer transformer = compiler.Compile(templateStream).Load();

                //set parameters
                //TODO:  AddExtensionObjects(XsltArgumentList arglist)  Those are a list of extension functions needs to be added
                //

                //  args.AddParam("TEMPLATE_URI", "", m_Engine.PublishingContext.ResolvedItem.Template.Id.ToString()); // Add the URI of the current template as the parameter
                //transformer.SetParameter(new QName("", "", "TEMPLATE_URI"), new XdmAtomicValue(m_Engine.PublishingContext.ResolvedItem.Template.Id.ToString()));

                //set paramters
                SetParameterCollection(transformer);

                transformer.InputXmlResolver = resolver;

                using (var results = new StringWriter()) // Create an XmlWriter which has the same output settings as the transformer
                {
                    using (var xmlwriter = XmlWriter.Create(results))
                    {
                        // Create a serializer.
                       // Serializer serializer = new Serializer();

                        // Set the root node of the source document to be the initial context node
                        //transformer.InitialTemplate = new QName("","","go");

                       // serializer.SetOutputWriter(new TextWriterDestination(xmlwriter));
                        //serializer.SetOutputProperty(Serializer.INDENT, "yes");

                        // serializer.SetOutputFile(Server.MapPath("test.html")); //for file

                        // Transform the source XML 
                        transformer.Run(new TextWriterDestination(xmlwriter));

                        Tridion.ContentManager.Templating.Item xsltOutputItem = package.CreateStringItem(GetContentType(template.Content), results.ToString());

                        if (m_OutputItemName == Package.OutputName)
                        {
                            xsltOutputItem.Properties[OUTPUT_BASE_URI] = template.Id; //mimic the behaviour of the DW mediator when the output item name is "Output"
                        }

                        package.PushItem(m_OutputItemName, xsltOutputItem); // Push the results into the package 
                    }
                }
            }
        }


        private void SetParameterCollection(XsltTransformer transformer)
        {


            //AddExtensionObjects(args); //adds the Tridion XSLT Helper as well as any other objects from a derived class

            transformer.SetParameter(new QName("", "", "TEMPLATE_URI"), new XdmAtomicValue(m_Engine.PublishingContext.ResolvedItem.Template.Id.ToString()));

            //AddPositioningVariables(args);

            if (m_GetFromPackage) // Loop through all of the entries in the package, and add them argument list as parameters
            {
                var keyBuilder = new StringBuilder();
                var packageEntries = m_Package.GetEntries();
                var addedKeys = new List<string>(packageEntries.Count);

                foreach (KeyValuePair<String, Tridion.ContentManager.Templating.Item> pair in packageEntries)
                {
                    if (String.IsNullOrEmpty(m_ParPrefix) || (!String.IsNullOrEmpty(m_ParPrefix) && pair.Key.ToLower().StartsWith(m_ParPrefix)))
                    {
                        keyBuilder.Append(String.IsNullOrEmpty(m_ParPrefix) ? pair.Key : pair.Key.Substring(m_ParPrefix.Length)); //if used a prefix strip the prefix before entering the argument

                        var key = keyBuilder.ToString();

                        if (!addedKeys.Contains(key)) //package may contain duplicate keys
                        {
                            switch (pair.Value.ContentType.Value)
                            {
                                case "text/plain": //Plain text items are added as strings	
                                    transformer.SetParameter(new QName("", "", key), new XdmAtomicValue(pair.Value.GetAsString()));
                                    break;
                                case "text/html": //HTML items are added as strings							
                                    //args.AddParam(key, "", pair.Value.GetAsString());
                                    transformer.SetParameter(new QName("", "", key), new XdmAtomicValue(pair.Value.GetAsString()));
                                    m_Logger.Debug(String.Format("Added parameter '{0}' as String", key));
                                    break;
                                default: //Other items are added as XmlDocuments								
                                    //args.AddParam(key, "", pair.Value.GetAsXmlDocument().ToString());
                                    transformer.SetParameter(new QName("", "", key), new XdmAtomicValue(pair.Value.GetAsXmlDocument().ToString()));
                                    m_Logger.Debug(String.Format("Added parameter '{0}' as XmlDocument", key));
                                    break;
                            }

                            addedKeys.Add(key);
                        }
                        else
                            Console.WriteLine(String.Format("argument collection already contains key: '{0}'", key));
                            Logger.Debug(String.Format("argument collection already contains key: '{0}'", key));

                        keyBuilder.Remove(0, keyBuilder.Length); //clear the stringbuilder before the next iteration
                    }
                }
            }

        }

        /// <summary>
        /// In a deriving class this method when overridden allows to add additional objects to the transformation 
        /// of which methods can be called from the xslt template.
        /// 
        /// Call base.AddExtensionObjects(arglist) to make use of the XsltTemplateHelper provided in this assembly
        /// and if configured to; load xslt helpers from a Tridion TBB
        /// </summary>
        /// <param name="arglist">the instance of XsltArgumentList being passed to the transformation</param>
        protected virtual void AddExtensionObjects(XsltArgumentList arglist)
        {
            arglist.AddExtensionObject("http://www.sdltridion.com/ps/XSLTHelper", new XsltTemplateHelper(m_Engine)); // Add the SDLTridionXsltTemplateHelper to the XSLT

            IEnumerable<XsltHelperInfo> helpers = GetHelpersFromTridion();

            if (helpers != null) AddExtensionObjectsFromTridion(arglist, helpers);
        }

        #endregion

        #region Tridion XSLT Helpers

        private IEnumerable<XsltHelperInfo> GetHelpersFromTridion()
        {
            var tridionHelpers = new List<XsltHelperInfo>();

            try
            {
                if (!String.IsNullOrEmpty(m_HelperInterface) && !String.IsNullOrEmpty(m_HelpersAssemblyTBB))
                {
                    var tbb = m_Engine.GetObject(m_HelpersAssemblyTBB) as TemplateBuildingBlock;

                    Logger.Debug(String.Format("Loaded template from Tridion: '{0}', assembly file name: '{1}'", tbb.Title, tbb.BinaryContent.Filename));

                    Assembly assemblyTBB = Assembly.Load(tbb.BinaryContent.GetByteArray());

                    Type[] types = assemblyTBB.GetTypes();
                    var helperTypes = new List<Type>();

                    foreach (Type t in types)
                    {
                        Type[] ints = t.FindInterfaces(
                            (type, criteria) => { return type.Name == criteria.ToString(); },
                            m_HelperInterface);

                        t.GetCustomAttributes(false);
                        if (ints.Length > 0) helperTypes.Add(t);
                    }

                    Logger.Debug(String.Format("Found {0} XSLT Helpers in assembly TBB", helperTypes.Count));

                    foreach (Type t in helperTypes)
                    {								//look for a constructor with no parameters
                        ConstructorInfo ci = t.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, new Type[] { }, null);

                        object helper = null;

                        if (ci != null)
                        {
                            helper = ci.Invoke(new object[] { });
                        }
                        else
                        {							//look for a constructor that accepts the Engine as the only parameter
                            ci = t.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, new Type[] { typeof(Engine) }, null);

                            if (ci != null)
                            {
                                helper = ci.Invoke(new object[] { m_Engine });
                            }
                            else
                            {
                                //look for a constructor that accepts the Engine and the Package								
                                ci = t.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, new Type[] { typeof(Engine), typeof(Package) }, null);

                                if (ci != null)
                                {
                                    helper = ci.Invoke(new object[] { m_Engine, m_Package });
                                }
                                else
                                {
                                    Logger.Warn(String.Format("type: '{0}' doesnt have a valid constructor!", t.FullName));
                                }
                            }
                        }

                        if (helper != null)
                        {
                            string helperNS = GetHelperNameSpace(t);

                            if (!String.IsNullOrEmpty(helperNS))
                            {
                                tridionHelpers.Add(new XsltHelperInfo(helper, helperNS));
                            }
                            else
                                Logger.Debug(String.Format("'{0}' doesnt have the '{1}' attribute, cannot use it in transformation", t.Name, HELPER_NS));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_Logger.Error("Couldnt load helpers from TBB. message: " + ex.Message, ex);
            }

            return tridionHelpers;
        }

        private void AddExtensionObjectsFromTridion(XsltArgumentList arglist, IEnumerable<XsltHelperInfo> helpers)
        {
            foreach (XsltHelperInfo helper in helpers)
            {
                arglist.AddExtensionObject(helper.HelperNamespace, helper.HelperObject);
                Logger.Debug(String.Format("Added '{0}' to the transformation with namespace: '{1}'", helper.HelperObject.GetType().Name, helper.HelperNamespace));
            }
        }

        private string GetHelperNameSpace(Type helperType)
        {
            object[] attributes = helperType.GetCustomAttributes(true);

            foreach (object att in attributes)
            {
                if (att.GetType().Name == HELPER_NS)
                {
                    return att.ToString();
                }
            }

            return null;
        }
        #endregion

        #region Helper Methods

        private void ReadParameters()
        {
            m_InputItemName = m_Package.GetValue("InputItem"); //Look for an input parameter called 'InputItem' otherwise load the Component or Page			

            if (string.IsNullOrEmpty(m_InputItemName))
            {
                m_Logger.Info("An item called 'InputItem' was not found in the package, searching for a Component instead");
                m_XmlInput = m_Package.GetByType(ContentType.Component);

                if (m_XmlInput == null)
                {
                    m_Logger.Info("An Component was not found in the package, searching for a Page instead");
                    m_XmlInput = m_Package.GetByType(ContentType.Page);
                }
                else
                    m_Logger.Debug("Using a Component as the input item");


            }
            else
            {
                m_XmlInput = m_Package.GetByName(m_InputItemName);

                if (m_XmlInput == null)
                {
                    m_Logger.Warn("Couldnt find package item with name: " + m_InputItemName);
                    throw new ArgumentException("Couldnt find package item with name: " + m_InputItemName, "InputItem");
                }
            }

            m_OutputItemName = m_Package.GetValue("OutputItem");

            if (string.IsNullOrEmpty(m_OutputItemName))
            {
                m_Logger.Info("An item called 'OutputItem' was not found in the package, using the default name 'Output' instead");
                m_OutputItemName = "Output";
            }

            m_GetFromPackage = m_Package.GetValue("packageParams") == "true";

            m_ParPrefix = m_Package.GetValue("parPrefix");

            m_outputType = m_Package.GetValue("outputType");

            if (String.IsNullOrEmpty(m_outputType)) m_outputType = String.Empty;
        }

        private ContentType GetContentType(string template)
        {
            ContentType type = null;

            switch (m_outputType.ToLower())
            {
                case "xml":
                    type = ContentType.Xml;
                    break;
                case "html":
                    type = ContentType.Html;
                    break;
                case "get from template":

                    string find = "xsl:output method=";

                    int pos = template.IndexOf(find);

                    if (pos > -1)
                    {
                        pos += find.Length + 1;

                        m_outputType = template.Substring(pos, ((template.IndexOf("\"", pos) - pos)));

                        m_Logger.Debug(String.Format("Rertived '{0}' as the output type from the template", m_outputType));

                        type = GetContentType(template);
                    }

                    break;
                default:
                    type = ContentType.Xhtml;
                    break;
            }

            return type;
        }

        private MemoryStream GetStringAsStream(string str)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(str));
        }

        #endregion
    }
}