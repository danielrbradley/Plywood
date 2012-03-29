using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood
{
    public class DeploymentException : Exception
    {
        public DeploymentException() : base() { }
        public DeploymentException(string message) : base(message) { }
        public DeploymentException(string message, Exception ex) : base(message, ex) { }
        public DeploymentException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class DeserialisationException : DeploymentException
    {
        public DeserialisationException() : base() { }
        public DeserialisationException(string message) : base(message) { }
        public DeserialisationException(string message, Exception ex) : base(message, ex) { }
        public DeserialisationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class ContextNotFoundException : DeploymentException
    {
        public ContextNotFoundException() : base() { }
        public ContextNotFoundException(string message) : base(message) { }
        public ContextNotFoundException(string message, Exception ex) : base(message, ex) { }
        public ContextNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class GroupNotFoundException : DeploymentException
    {
        public GroupNotFoundException() : base() { }
        public GroupNotFoundException(string message) : base(message) { }
        public GroupNotFoundException(string message, Exception ex) : base(message, ex) { }
        public GroupNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class AppNotFoundException : DeploymentException
    {
        public AppNotFoundException() : base() { }
        public AppNotFoundException(string message) : base(message) { }
        public AppNotFoundException(string message, Exception ex) : base(message, ex) { }
        public AppNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class VersionNotFoundException : DeploymentException
    {
        public VersionNotFoundException() : base() { }
        public VersionNotFoundException(string message) : base(message) { }
        public VersionNotFoundException(string message, Exception ex) : base(message, ex) { }
        public VersionNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class RoleNotFoundException : DeploymentException
    {
        public RoleNotFoundException() : base() { }
        public RoleNotFoundException(string message) : base(message) { }
        public RoleNotFoundException(string message, Exception ex) : base(message, ex) { }
        public RoleNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class InstanceNotFoundException : DeploymentException
    {
        public InstanceNotFoundException() : base() { }
        public InstanceNotFoundException(string message) : base(message) { }
        public InstanceNotFoundException(string message, Exception ex) : base(message, ex) { }
        public InstanceNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class LogEntryNotFoundException : DeploymentException
    {
        public LogEntryNotFoundException() : base() { }
        public LogEntryNotFoundException(string message) : base(message) { }
        public LogEntryNotFoundException(string message, Exception ex) : base(message, ex) { }
        public LogEntryNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class IndexEntryNotFoundException : DeploymentException
    {
        public IndexEntryNotFoundException() : base() { }
        public IndexEntryNotFoundException(string message) : base(message) { }
        public IndexEntryNotFoundException(string message, Exception ex) : base(message, ex) { }
        public IndexEntryNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class NoTargetAppVersionSetException : DeploymentException
    {
        public NoTargetAppVersionSetException() : base() { }
        public NoTargetAppVersionSetException(string message) : base(message) { }
        public NoTargetAppVersionSetException(string message, Exception ex) : base(message, ex) { }
        public NoTargetAppVersionSetException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class ConfigurationException : DeploymentException
    {
        public ConfigurationException() : base() { }
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception ex) : base(message, ex) { }
        public ConfigurationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class HooksParserException : Plywood.DeploymentException
    {
        public HooksParserException() : base() { }
        public HooksParserException(string message) : base(message) { }
        public HooksParserException(string message, Exception ex) : base(message, ex) { }
        public HooksParserException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

}
