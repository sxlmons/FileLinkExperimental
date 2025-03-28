namespace CloudFileClient.Protocol
{
    /// <summary>
    /// Contains definitions for all command codes used in the protocol.
    /// </summary>
    public static class Commands
    {
        /// <summary>
        /// Contains static definitions for all command codes used in the CloudFileServer protocol.
        /// </summary>
        public static class CommandCode
        {
            // Authentication Commands (100-199)
            /// <summary>
            /// Command code for login request from client.
            /// </summary>
            public const int LOGIN_REQUEST = 100;

            /// <summary>
            /// Command code for login response from server.
            /// </summary>
            public const int LOGIN_RESPONSE = 101;

            /// <summary>
            /// Command code for logout request from client.
            /// </summary>
            public const int LOGOUT_REQUEST = 102;

            /// <summary>
            /// Command code for logout response from server.
            /// </summary>
            public const int LOGOUT_RESPONSE = 103;
            
            /// <summary>
            /// Command code for account creation request from client.
            /// </summary>
            public const int CREATE_ACCOUNT_REQUEST = 110;
            
            /// <summary>
            /// Command code for account creation response from server.
            /// </summary>
            public const int CREATE_ACCOUNT_RESPONSE = 111;

            // File Operations (200-299)
            /// <summary>
            /// Command code for file list request from client.
            /// </summary>
            public const int FILE_LIST_REQUEST = 200;

            /// <summary>
            /// Command code for file list response from server.
            /// </summary>
            public const int FILE_LIST_RESPONSE = 201;

            /// <summary>
            /// Command code for file upload initialization request from client.
            /// </summary>
            public const int FILE_UPLOAD_INIT_REQUEST = 210;

            /// <summary>
            /// Command code for file upload initialization response from server.
            /// </summary>
            public const int FILE_UPLOAD_INIT_RESPONSE = 211;

            /// <summary>
            /// Command code for file upload chunk request from client.
            /// </summary>
            public const int FILE_UPLOAD_CHUNK_REQUEST = 212;

            /// <summary>
            /// Command code for file upload chunk response from server.
            /// </summary>
            public const int FILE_UPLOAD_CHUNK_RESPONSE = 213;

            /// <summary>
            /// Command code for file upload completion request from client.
            /// </summary>
            public const int FILE_UPLOAD_COMPLETE_REQUEST = 214;

            /// <summary>
            /// Command code for file upload completion response from server.
            /// </summary>
            public const int FILE_UPLOAD_COMPLETE_RESPONSE = 215;

            /// <summary>
            /// Command code for file download initialization request from client.
            /// </summary>
            public const int FILE_DOWNLOAD_INIT_REQUEST = 220;

            /// <summary>
            /// Command code for file download initialization response from server.
            /// </summary>
            public const int FILE_DOWNLOAD_INIT_RESPONSE = 221;

            /// <summary>
            /// Command code for file download chunk request from client.
            /// </summary>
            public const int FILE_DOWNLOAD_CHUNK_REQUEST = 222;

            /// <summary>
            /// Command code for file download chunk response from server.
            /// </summary>
            public const int FILE_DOWNLOAD_CHUNK_RESPONSE = 223;

            /// <summary>
            /// Command code for file download completion request from client.
            /// </summary>
            public const int FILE_DOWNLOAD_COMPLETE_REQUEST = 224;

            /// <summary>
            /// Command code for file download completion response from server.
            /// </summary>
            public const int FILE_DOWNLOAD_COMPLETE_RESPONSE = 225;

            /// <summary>
            /// Command code for file deletion request from client.
            /// </summary>
            public const int FILE_DELETE_REQUEST = 230;

            /// <summary>
            /// Command code for file deletion response from server.
            /// </summary>
            public const int FILE_DELETE_RESPONSE = 231;
            
             // Directory Operations (240-249)
            /// <summary>
            /// Command code for directory creation request from client.
            /// </summary>
            public const int DIRECTORY_CREATE_REQUEST = 240;

            /// <summary>
            /// Command code for directory creation response from server.
            /// </summary>
            public const int DIRECTORY_CREATE_RESPONSE = 241;

            /// <summary>
            /// Command code for directory list request from client.
            /// </summary>
            public const int DIRECTORY_LIST_REQUEST = 242;

            /// <summary>
            /// Command code for directory list response from server.
            /// </summary>
            public const int DIRECTORY_LIST_RESPONSE = 243;

            /// <summary>
            /// Command code for directory rename request from client.
            /// </summary>
            public const int DIRECTORY_RENAME_REQUEST = 244;

            /// <summary>
            /// Command code for directory rename response from server.
            /// </summary>
            public const int DIRECTORY_RENAME_RESPONSE = 245;

            /// <summary>
            /// Command code for directory deletion request from client.
            /// </summary>
            public const int DIRECTORY_DELETE_REQUEST = 246;

            /// <summary>
            /// Command code for directory deletion response from server.
            /// </summary>
            public const int DIRECTORY_DELETE_RESPONSE = 247;

            /// <summary>
            /// Command code for file move request from client.
            /// </summary>
            public const int FILE_MOVE_REQUEST = 248;

            /// <summary>
            /// Command code for file move response from server.
            /// </summary>
            public const int FILE_MOVE_RESPONSE = 249;

            /// <summary>
            /// Command code for directory contents request from client.
            /// </summary>
            public const int DIRECTORY_CONTENTS_REQUEST = 250;

            /// <summary>
            /// Command code for directory contents response from server.
            /// </summary>
            public const int DIRECTORY_CONTENTS_RESPONSE = 251;

            // Status Responses (300-399)
            /// <summary>
            /// Command code for general success response.
            /// </summary>
            public const int SUCCESS = 300;

            /// <summary>
            /// Command code for general error response.
            /// </summary>
            public const int ERROR = 301;

            /// <summary>
            /// Gets the string name of a command code for logging and debugging.
            /// </summary>
            /// <param name="code">The command code.</param>
            /// <returns>A human-readable string representation of the command code.</returns>
            public static string GetCommandName(int code)
            {
                return code switch
                {
                    LOGIN_REQUEST => "LOGIN_REQUEST",
                    LOGIN_RESPONSE => "LOGIN_RESPONSE",
                    LOGOUT_REQUEST => "LOGOUT_REQUEST",
                    LOGOUT_RESPONSE => "LOGOUT_RESPONSE",
                    CREATE_ACCOUNT_REQUEST => "CREATE_ACCOUNT_REQUEST",
                    CREATE_ACCOUNT_RESPONSE => "CREATE_ACCOUNT_RESPONSE",
                    FILE_LIST_REQUEST => "FILE_LIST_REQUEST",
                    FILE_LIST_RESPONSE => "FILE_LIST_RESPONSE",
                    FILE_UPLOAD_INIT_REQUEST => "FILE_UPLOAD_INIT_REQUEST",
                    FILE_UPLOAD_INIT_RESPONSE => "FILE_UPLOAD_INIT_RESPONSE",
                    FILE_UPLOAD_CHUNK_REQUEST => "FILE_UPLOAD_CHUNK_REQUEST",
                    FILE_UPLOAD_CHUNK_RESPONSE => "FILE_UPLOAD_CHUNK_RESPONSE",
                    FILE_UPLOAD_COMPLETE_REQUEST => "FILE_UPLOAD_COMPLETE_REQUEST",
                    FILE_UPLOAD_COMPLETE_RESPONSE => "FILE_UPLOAD_COMPLETE_RESPONSE",
                    FILE_DOWNLOAD_INIT_REQUEST => "FILE_DOWNLOAD_INIT_REQUEST",
                    FILE_DOWNLOAD_INIT_RESPONSE => "FILE_DOWNLOAD_INIT_RESPONSE",
                    FILE_DOWNLOAD_CHUNK_REQUEST => "FILE_DOWNLOAD_CHUNK_REQUEST",
                    FILE_DOWNLOAD_CHUNK_RESPONSE => "FILE_DOWNLOAD_CHUNK_RESPONSE",
                    FILE_DOWNLOAD_COMPLETE_REQUEST => "FILE_DOWNLOAD_COMPLETE_REQUEST",
                    FILE_DOWNLOAD_COMPLETE_RESPONSE => "FILE_DOWNLOAD_COMPLETE_RESPONSE",
                    FILE_DELETE_REQUEST => "FILE_DELETE_REQUEST",
                    FILE_DELETE_RESPONSE => "FILE_DELETE_RESPONSE",
                    // Directory operation commands
                    DIRECTORY_CREATE_REQUEST => "DIRECTORY_CREATE_REQUEST",
                    DIRECTORY_CREATE_RESPONSE => "DIRECTORY_CREATE_RESPONSE",
                    DIRECTORY_LIST_REQUEST => "DIRECTORY_LIST_REQUEST",
                    DIRECTORY_LIST_RESPONSE => "DIRECTORY_LIST_RESPONSE",
                    DIRECTORY_RENAME_REQUEST => "DIRECTORY_RENAME_REQUEST",
                    DIRECTORY_RENAME_RESPONSE => "DIRECTORY_RENAME_RESPONSE",
                    DIRECTORY_DELETE_REQUEST => "DIRECTORY_DELETE_REQUEST",
                    DIRECTORY_DELETE_RESPONSE => "DIRECTORY_DELETE_RESPONSE",
                    FILE_MOVE_REQUEST => "FILE_MOVE_REQUEST",
                    FILE_MOVE_RESPONSE => "FILE_MOVE_RESPONSE",
                    DIRECTORY_CONTENTS_REQUEST => "DIRECTORY_CONTENTS_REQUEST",
                    DIRECTORY_CONTENTS_RESPONSE => "DIRECTORY_CONTENTS_RESPONSE",
                    SUCCESS => "SUCCESS",
                    ERROR => "ERROR",
                    _ => $"UNKNOWN({code})"
                };
            }

            /// <summary>
            /// Gets the corresponding response command code for a request command code.
            /// </summary>
            /// <param name="requestCode">The request command code.</param>
            /// <returns>The corresponding response command code or ERROR if not found.</returns>
            public static int GetResponseCommandCode(int requestCode)
            {
                return requestCode switch
                {
                    LOGIN_REQUEST => LOGIN_RESPONSE,
                    LOGOUT_REQUEST => LOGOUT_RESPONSE,
                    CREATE_ACCOUNT_REQUEST => CREATE_ACCOUNT_RESPONSE,
                    FILE_LIST_REQUEST => FILE_LIST_RESPONSE,
                    FILE_UPLOAD_INIT_REQUEST => FILE_UPLOAD_INIT_RESPONSE,
                    FILE_UPLOAD_CHUNK_REQUEST => FILE_UPLOAD_CHUNK_RESPONSE,
                    FILE_UPLOAD_COMPLETE_REQUEST => FILE_UPLOAD_COMPLETE_RESPONSE,
                    FILE_DOWNLOAD_INIT_REQUEST => FILE_DOWNLOAD_INIT_RESPONSE,
                    FILE_DOWNLOAD_CHUNK_REQUEST => FILE_DOWNLOAD_CHUNK_RESPONSE,
                    FILE_DOWNLOAD_COMPLETE_REQUEST => FILE_DOWNLOAD_COMPLETE_RESPONSE,
                    FILE_DELETE_REQUEST => FILE_DELETE_RESPONSE,
                    // Directory operation commands
                    DIRECTORY_CREATE_REQUEST => DIRECTORY_CREATE_RESPONSE,
                    DIRECTORY_LIST_REQUEST => DIRECTORY_LIST_RESPONSE,
                    DIRECTORY_RENAME_REQUEST => DIRECTORY_RENAME_RESPONSE,
                    DIRECTORY_DELETE_REQUEST => DIRECTORY_DELETE_RESPONSE,
                    FILE_MOVE_REQUEST => FILE_MOVE_RESPONSE,
                    DIRECTORY_CONTENTS_REQUEST => DIRECTORY_CONTENTS_RESPONSE,
                    
                    _ => ERROR
                };
            }
        }
    }
}