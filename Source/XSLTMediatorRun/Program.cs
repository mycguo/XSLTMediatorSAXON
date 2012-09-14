using System;
using System.Collections.Generic;
using Tridion.ContentManager.Templating;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.XPath;
using System.Xml;
using System.Collections;
using System.Net;
using Saxon.Api;



namespace Tridion.Extensions.Mediators
{
    /// <summary>
    /// Run XSLT Identity Transfrom,shows the vender and version info
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {

            String s1 = @"<xsl:stylesheet version='2.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'><xsl:output method='xml' indent='yes'/><xsl:template match='/'>";
            String s2 = "This is running an XSLT engine by <xsl:value-of select=\"system-property('xsl:vendor')\" />  <a href=\"{system-property('xsl:vendor-url')}\"><xsl:value-of select=\"system-property('xsl:vendor-url')\" /></a> implementing XSLT v<xsl:value-of select=\"system-property('xsl:version')\" /> ";
            String s3 = @"\n<xsl:apply-templates/></xsl:template><xsl:template match='@*|node()'> ";
            String s4 = @"<xsl:copy><xsl:apply-templates select='@*|node()'/></xsl:copy></xsl:template></xsl:stylesheet>";

            String str = s1 + s2 + s3  + s4;

            //Run Saxon
            //new SaxonTransform().doTransform(args, "Transform");


            //Run Saxon s9 api
            // Create a Processor instance.
            Processor processor = new Processor();
            

            // Load the source document.
            XdmNode input = processor.NewDocumentBuilder().Build(XmlReader.Create(new StringReader(str)));

            XsltCompiler compiler = processor.NewXsltCompiler();
            //although it is not required in this example, we need to set otherwise exception will be thrown
            compiler.BaseUri = new Uri("http://dummyurl.com");
            // Create a transformer for the stylesheet.
            XsltTransformer transformer = compiler.Compile(XmlReader.Create(new StringReader(str))).Load();

            // BaseOutputUri is only necessary for xsl:result-document.
            //transformer.BaseOutputUri = new Uri("http://dummyurl.com");
            // Set the root node of the source document to be the initial context node.
            transformer.InitialContextNode = input;

            transformer.SetParameter(new QName("", "", "maxmin"), new XdmAtomicValue("parm1"));
            transformer.SetParameter(new QName("", "", "pricestock"), new XdmAtomicValue("parm2"));

            // Create a serializer.
            Serializer serializer = new Serializer();

            // Set the root node of the source document to be the initial context node
            //transformer.InitialTemplate = new QName("","","go");

            serializer.SetOutputWriter(Console.Out);
            serializer.SetOutputProperty(Serializer.INDENT, "yes");

            // serializer.SetOutputFile(Server.MapPath("test.html")); //for file

            // Transform the source XML to System.out.
            transformer.Run(serializer);

            //Call the SaxonMediator
            SaxonMediator mediator = new SaxonMediator();
            
            //mediator.Transform(null, Template template, Package package)


            //wait for user to exit
            Console.ReadLine();

        }



    }
}
