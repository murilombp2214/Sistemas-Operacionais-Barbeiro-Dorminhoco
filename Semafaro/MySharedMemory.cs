using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace Semafaro
{
    public class MySharedMemory
    {
        public MemoryMappedFile MapFile { get; private set; }
        public MemoryMappedViewAccessor Acessor { get => MapFile.CreateViewAccessor(); }
        public string MapName { get; private set; } = "MAP-SO-23";

        /// <summary>
        /// Cria uma espaço de memoria compartilhado
        /// </summary>
        public void CreatePipe()
        {
            MapFile = MemoryMappedFile.CreateNew(mapName: MapName, capacity: 1024 * 10);
        }

        /// <summary>
        /// Obtem o espaço de memoria compartilado
        /// </summary>
        public void OpenPipe()
        {
            MapFile = MemoryMappedFile.OpenExisting(MapName);
        }

    }
}
