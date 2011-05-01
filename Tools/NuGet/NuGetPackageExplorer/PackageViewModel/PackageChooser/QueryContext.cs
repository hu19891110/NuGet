﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PackageExplorerViewModel {
    internal class QueryContext<T> {

        private readonly IQueryable<T> _source;
        private readonly int _bufferSize;
        private readonly IEqualityComparer<T> _comparer;
        private readonly int _pageSize;
        private int _skip, _nextSkip;
        private readonly Stack<int> _skipHistory = new Stack<int>();
        private readonly Lazy<int> _totalItemCount;

        public int PageIndex {
            get {
                return _skipHistory.Count;
            }
        }

        public int BeginPackage {
            get {
                return _skip+1;
            }
        }

        public int EndPackage {
            get {
                return _nextSkip;
            }
        }

        public int TotalItemCount {
            get {
                return _totalItemCount.Value;
            }
        }

        public QueryContext(IQueryable<T> source, int pageSize, int bufferSize, IEqualityComparer<T> comparer) {
            _source = source;
            _bufferSize = bufferSize;
            _comparer = comparer;
            _pageSize = pageSize;
            _totalItemCount = new Lazy<int>(_source.Count);
        }

        public IEnumerable<T> GetItemsForCurrentPage() {
            T[] buffer = null;
            int skipCursor = _nextSkip = _skip;
            int head = 0;
            for (int i = 0; i < _pageSize; i++) {
                bool firstItem = true;
                T lastItem = default(T);
                while (true) {
                    if (buffer == null || head >= buffer.Length) {
                        // read the next batch
                        buffer = _source.Skip(skipCursor).Take(_bufferSize).ToArray();
                        if (buffer.Length == 0) {
                            // if no item returned, we have reached the end.
                            yield break;
                        }

                        head = 0;
                        skipCursor += buffer.Length;
                    }

                    if (firstItem || _comparer.Equals(buffer[head], lastItem)) {
                        yield return buffer[head];
                        lastItem = buffer[head];
                        head++;
                        firstItem = false;
                        _nextSkip++;
                    }
                    else {
                        break;
                    }
                }
            }
        }

        public bool MoveFirst() {
            _skipHistory.Clear();
            _skip = _nextSkip = 0;
            return true;
        }

        public bool MoveNext() {
            if (_nextSkip != _skip && _nextSkip < TotalItemCount) {
                _skipHistory.Push(_skip);
                _skip = _nextSkip;
                return true;
            }

            return false;
        }

        public bool MovePrevious() {
            if (PageIndex > 0) {
                _nextSkip = _skip;
                _skip = _skipHistory.Pop();
                return true;
            }
            return false;
        }

        public bool MoveLast() {
            return MovePrevious();
        }
    }
}