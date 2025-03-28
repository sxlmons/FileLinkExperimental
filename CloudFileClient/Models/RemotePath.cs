using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudFileClient.Models
{
    /// <summary>
    /// Utility class for representing and manipulating paths on the remote server.
    /// </summary>
    public class RemotePath
    {
        private readonly List<PathSegment> _segments = new List<PathSegment>();
        
        /// <summary>
        /// Gets the current directory ID.
        /// </summary>
        public string CurrentDirectoryId => _segments.Count > 0 ? _segments.Last().DirectoryId : null;
        
        /// <summary>
        /// Gets the current directory name.
        /// </summary>
        public string CurrentDirectoryName => _segments.Count > 0 ? _segments.Last().DirectoryName : "root";
        
        /// <summary>
        /// Gets a value indicating whether this path is the root path.
        /// </summary>
        public bool IsRoot => _segments.Count == 0;
        
        /// <summary>
        /// Gets the path as a string.
        /// </summary>
        public string Path => "/" + string.Join("/", _segments.Select(s => s.DirectoryName));
        
        /// <summary>
        /// Initializes a new instance of the RemotePath class.
        /// </summary>
        public RemotePath()
        {
        }
        
        /// <summary>
        /// Navigates to a child directory.
        /// </summary>
        /// <param name="directoryId">The ID of the child directory.</param>
        /// <param name="directoryName">The name of the child directory.</param>
        public void NavigateToChild(string directoryId, string directoryName)
        {
            if (string.IsNullOrEmpty(directoryId))
                throw new ArgumentException("Directory ID cannot be empty.", nameof(directoryId));
            
            if (string.IsNullOrEmpty(directoryName))
                throw new ArgumentException("Directory name cannot be empty.", nameof(directoryName));
            
            _segments.Add(new PathSegment(directoryId, directoryName));
        }
        
        /// <summary>
        /// Navigates to the parent directory.
        /// </summary>
        /// <returns>True if navigation was successful, false if already at the root.</returns>
        public bool NavigateToParent()
        {
            if (_segments.Count == 0)
                return false;
            
            _segments.RemoveAt(_segments.Count - 1);
            return true;
        }
        
        /// <summary>
        /// Navigates to the root.
        /// </summary>
        public void NavigateToRoot()
        {
            _segments.Clear();
        }
        
        /// <summary>
        /// Gets the parent directory ID.
        /// </summary>
        /// <returns>The parent directory ID, or null if at the root.</returns>
        public string GetParentDirectoryId()
        {
            if (_segments.Count <= 1)
                return null;
            
            return _segments[_segments.Count - 2].DirectoryId;
        }
        
        /// <summary>
        /// Gets the path segments.
        /// </summary>
        /// <returns>An enumerable of path segments.</returns>
        public IEnumerable<PathSegment> GetSegments()
        {
            return _segments.AsReadOnly();
        }
        
        /// <summary>
        /// Gets a breadcrumb representation of the path.
        /// </summary>
        /// <returns>A string with breadcrumb navigation.</returns>
        public string GetBreadcrumb()
        {
            return "root" + (_segments.Count > 0 ? " > " + string.Join(" > ", _segments.Select(s => s.DirectoryName)) : "");
        }
        
        /// <summary>
        /// Represents a segment in a remote path.
        /// </summary>
        public class PathSegment
        {
            /// <summary>
            /// Gets the directory ID.
            /// </summary>
            public string DirectoryId { get; }
            
            /// <summary>
            /// Gets the directory name.
            /// </summary>
            public string DirectoryName { get; }
            
            /// <summary>
            /// Initializes a new instance of the PathSegment class.
            /// </summary>
            /// <param name="directoryId">The directory ID.</param>
            /// <param name="directoryName">The directory name.</param>
            public PathSegment(string directoryId, string directoryName)
            {
                DirectoryId = directoryId;
                DirectoryName = directoryName;
            }
        }
    }
}