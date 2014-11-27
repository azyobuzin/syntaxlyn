using System;
using Microsoft.CodeAnalysis;

namespace Syntaxlyn.Core
{
    public class StartedParsingDocumentEventArgs : EventArgs
    {
        public StartedParsingDocumentEventArgs(Document document)
        {
            this.Document = document;
        }

        public Document Document { get; private set; }
    }
}
