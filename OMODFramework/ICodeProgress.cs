/*
    Copyright (C) 2019-2020  erri120

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace OMODFramework
{
    public interface ICodeProgress : SevenZip.ICodeProgress
    {
        /// <summary>
        /// Initialization
        /// </summary>
        /// <param name="totalSize">Total size of the archive in bytes</param>
        /// <param name="compressing">Whether you are compressing or decompressing</param>
        void Init(long totalSize, bool compressing);

        /// <summary>
        /// Called after the coding is done
        /// </summary>
        void Dispose();
    }
}
