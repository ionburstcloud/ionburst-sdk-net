// Copyright Ionburst Limited 2020
using System;

namespace Ionburst.SDK
{
    class IonburstServiceUnavailableException : Exception
    {
        public IonburstServiceUnavailableException()
        {
        }

        public IonburstServiceUnavailableException(string message)
            : base(message)
        {
        }

        public IonburstServiceUnavailableException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    class IonburstServiceException : Exception
    {
        public IonburstServiceException()
        {
        }

        public IonburstServiceException(string message)
            : base(message)
        {
        }

        public IonburstServiceException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    class IonburstUriUndefinedException : Exception
    {
        public IonburstUriUndefinedException()
        {
        }

        public IonburstUriUndefinedException(string message)
            : base(message)
        {
        }

        public IonburstUriUndefinedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    class IonburstCredentialsUndefinedException : Exception
    {
        public IonburstCredentialsUndefinedException()
        {
        }

        public IonburstCredentialsUndefinedException(string message)
            : base(message)
        {
        }

        public IonburstCredentialsUndefinedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    class IonburstCredentialsException : Exception
    {
        public IonburstCredentialsException()
        {
        }

        public IonburstCredentialsException(string message)
            : base(message)
        {
        }

        public IonburstCredentialsException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
