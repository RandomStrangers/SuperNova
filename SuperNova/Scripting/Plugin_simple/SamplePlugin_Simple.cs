﻿namespace SuperNova.Scripting.Plugin_simple
{
    internal class SamplePlugin_Simple 
    {
        public string SimplePluginSkeleton
        {
            get
            {
                return @"' This is an example simpleplugin source!
Imports System

Namespace SuperNova
\tPublic Class {0}
\t\tInherits Plugin

\t\tPublic Overrides ReadOnly Property name() As String
\t\t\tGet
\t\t\t\tReturn ""{0}""
\t\t\tEnd Get
\t\t End Property
\t\tPublic Overrides ReadOnly Property SuperNova_Version() As String
\t\t\tGet
\t\t\t\tReturn ""{2}""
\t\t\tEnd Get
\t\t End Property
\t\tPublic Overrides ReadOnly Property welcome() As String
\t\t\tGet
\t\t\t\tReturn ""Loaded Message!""
\t\t\tEnd Get
\t\t End Property
\t\t\tDoesn't actually need the welcome message
\t\tPublic Overrides ReadOnly Property creator() As String
\t\t\tGet
\t\t\t\tReturn ""{1}""
\t\t\tEnd Get
\t\t End Property

\t\tPublic Overrides Sub Load(startup As Boolean)
\t\t\t' LOAD YOUR SIMPLE PLUGIN WITH EVENTS OR OTHER THINGS!
\t\tEnd Sub
                        
\t\tPublic Overrides Sub Unload(shutdown As Boolean)
\t\t\t' UNLOAD YOUR SIMPLE PLUGIN BY SAVING FILES OR DISPOSING OBJECTS!
\t\tEnd Sub
                        
\t\tPublic Overrides Sub Help(p As Player)
\t\t\t' HELP INFO!
\t\tEnd Sub
\tEnd Class
End Namespace";
            }
        }
    }
}
