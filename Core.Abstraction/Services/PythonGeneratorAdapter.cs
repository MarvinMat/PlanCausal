using System;
using System.Collections;
using System.Collections.Generic;

namespace Core.Abstraction.Services
{
    public class PythonGeneratorAdapter<T> : IEnumerable<T>
    {
        private dynamic pythonEnumerator;

        public PythonGeneratorAdapter(dynamic pythonEnumerator)
        {
            this.pythonEnumerator = pythonEnumerator;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new PythonGeneratorEnumerator<T>(pythonEnumerator);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class PythonGeneratorEnumerator<T> : IEnumerator<T>
    {
        private dynamic pythonEnumerator;
        public PythonGeneratorEnumerator(dynamic pythonEnumerator)
        {
            this.pythonEnumerator = pythonEnumerator;
        }

        public T Current => pythonEnumerator.Current();

        object IEnumerator.Current => pythonEnumerator.Current();

        public void Dispose()
        {
            pythonEnumerator.Dispose();
        }

        public bool MoveNext()
        {
            return pythonEnumerator.MoveNext();
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
    }
}
