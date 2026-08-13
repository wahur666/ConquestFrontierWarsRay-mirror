namespace ConquestFrontierWarsRay.Data.DosFile;

internal static class UtfConstants {
	public const uint Identifier = 0x20465455;
	public const uint Version = 0x101;
	public const string RootName = "\\";
}

internal abstract class UtfNode {
	protected UtfNode(string name, FileAttributes attributes) {
		Name = name;
		Attributes = attributes;
		CreationTimeUtc = DateTime.UtcNow;
		LastAccessTimeUtc = DateTime.UtcNow;
		LastWriteTimeUtc = DateTime.UtcNow;
	}

	public string Name { get; set; }

	public FileAttributes Attributes { get; set; }

	public DateTime CreationTimeUtc { get; set; }

	public DateTime LastAccessTimeUtc { get; set; }

	public DateTime LastWriteTimeUtc { get; set; }

	public abstract bool IsDirectory { get; }
}

internal sealed partial class UtfDirectoryNode : UtfNode {
	public UtfDirectoryNode(string name)
		: base(name, FileAttributes.Directory) {
	}

	public Dictionary<string, UtfNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);

	public override bool IsDirectory => true;
}

internal sealed partial class UtfFileNode : UtfNode {
	public UtfFileNode(string name, byte[] data)
		: base(name, FileAttributes.Normal) {
		Data = data;
	}

	public byte[] Data { get; set; }

	public override bool IsDirectory => false;
}

internal static class UtfArchive {
	private const int HeaderSize = 56;
	private const int DirectoryEntrySize = 44;

	public static UtfDirectoryNode Parse(ReadOnlySpan<byte> data) {
		using var stream = new MemoryStream(data.ToArray(), writable: false);
		using var reader = new BinaryReader(stream);

		var identifier = reader.ReadUInt32();
		var version = reader.ReadUInt32();
		if (identifier != UtfConstants.Identifier || version != UtfConstants.Version) {
			throw new InvalidDataException("Not a supported UTF container.");
		}

		var directoryOffset = reader.ReadUInt32();
		var directorySize = reader.ReadUInt32();
		_ = reader.ReadUInt32();
		var dirEntrySize = reader.ReadUInt32();
		var namesOffset = reader.ReadUInt32();
		var nameSpaceSize = reader.ReadUInt32();
		var nameSpaceUsed = reader.ReadUInt32();
		var dataStartOffset = reader.ReadUInt32();
		_ = reader.ReadUInt32();
		_ = reader.ReadUInt32();
		var lastWriteLow = reader.ReadUInt32();
		var lastWriteHigh = reader.ReadUInt32();
		var lastWriteTime = DateTime.FromFileTimeUtc(((long)lastWriteHigh << 32) | lastWriteLow);

		stream.Position = namesOffset;
		var names = reader.ReadBytes((int)nameSpaceSize);

		stream.Position = directoryOffset;
		var entryCount = (int)(directorySize / dirEntrySize);
		var entries = new UtfEntryRecord[entryCount];
		for (var i = 0; i < entryCount; i++) {
			entries[i] = UtfEntryRecord.Read(reader, dataStartOffset, dirEntrySize);
		}

		var root = BuildTree(entries, 0, names, data, dataStartOffset);
		root.LastWriteTimeUtc = lastWriteTime;
		return root;
	}

	public static byte[] Serialize(UtfDirectoryNode root) {
		var nodes = new List<UtfEntryPlan>();
		var rootPlan = new UtfEntryPlan(root);
		nodes.Add(rootPlan);
		AddNodes(root, rootPlan, nodes);

		var nameOffsets =
			BuildNameBuffer(nodes.Select(plan => plan.Node.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
				out var nameBuffer, out var nameSpaceUsed);

		var directoryOffset = HeaderSize + nameBuffer.Length;
		var directorySize = nodes.Count * DirectoryEntrySize;
		var dataStartOffset = Align4(directoryOffset + directorySize);

		var currentDataOffset = 0;
		foreach (var plan in nodes) {
			plan.EntryOffset = (uint)(plan.Index * DirectoryEntrySize);
			plan.NameOffset = nameOffsets[plan.Node.Name];

			if (plan.Node is UtfFileNode file) {
				plan.DataOffset = (uint)currentDataOffset;
				plan.DataLength = (uint)file.Data.Length;
				currentDataOffset += file.Data.Length;
			}
		}

		foreach (var plan in nodes) {
			if (plan.Node is not UtfDirectoryNode directory) {
				continue;
			}

			var children = directory.Children.Values
				.Select(child => nodes.First(node => ReferenceEquals(node.Node, child)))
				.ToArray();

			if (children.Length > 0) {
				plan.DataOffset = children[0].EntryOffset;
			}

			for (var i = 0; i < children.Length; i++) {
				children[i].NextOffset = i == children.Length - 1 ? 0 : children[i + 1].EntryOffset;
			}
		}

		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);

		writer.Write(UtfConstants.Identifier);
		writer.Write(UtfConstants.Version);
		writer.Write((uint)directoryOffset);
		writer.Write((uint)directorySize);
		writer.Write(0u);
		writer.Write((uint)DirectoryEntrySize);
		writer.Write((uint)HeaderSize);
		writer.Write((uint)nameBuffer.Length);
		writer.Write((uint)nameSpaceUsed);
		writer.Write((uint)dataStartOffset);
		writer.Write(0u);
		writer.Write(0u);

		var lastWrite = root.LastWriteTimeUtc == default ? DateTime.UtcNow : root.LastWriteTimeUtc;
		var fileTime = lastWrite.ToFileTimeUtc();
		writer.Write((uint)(fileTime & 0xFFFFFFFF));
		writer.Write((uint)(fileTime >> 32));

		writer.Write(nameBuffer);

		foreach (var plan in nodes.OrderBy(plan => plan.Index)) {
			UtfEntryRecord.Write(writer, plan, dataStartOffset);
		}

		while (stream.Length < dataStartOffset) {
			writer.Write((byte)0);
		}

		foreach (var plan in nodes) {
			if (plan.Node is UtfFileNode file) {
				writer.Write(file.Data);
			}
		}

		return stream.ToArray();
	}

	private static UtfDirectoryNode BuildTree(
		IReadOnlyList<UtfEntryRecord> entries,
		int entryIndex,
		byte[] names,
		ReadOnlySpan<byte> fileData,
		uint dataStartOffset) {
		var rootEntry = entries[entryIndex];
		var root = new UtfDirectoryNode(GetName(names, rootEntry.NameOffset)) {
			CreationTimeUtc = rootEntry.CreationTimeUtc,
			LastAccessTimeUtc = rootEntry.LastAccessTimeUtc,
			LastWriteTimeUtc = rootEntry.LastWriteTimeUtc
		};

		PopulateChildren(root, rootEntry, entries, names, fileData, dataStartOffset);
		return root;
	}

	private static void PopulateChildren(
		UtfDirectoryNode parent,
		UtfEntryRecord parentEntry,
		IReadOnlyList<UtfEntryRecord> entries,
		byte[] names,
		ReadOnlySpan<byte> fileData,
		uint dataStartOffset) {
		if (parentEntry.DataOffset == 0) {
			return;
		}

		var nextEntryIndex = (int)(parentEntry.DataOffset / DirectoryEntrySize);
		while (nextEntryIndex > 0 && nextEntryIndex < entries.Count) {
			var entry = entries[nextEntryIndex];
			var name = GetName(names, entry.NameOffset);
			UtfNode node;

			if ((entry.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
				var directory = new UtfDirectoryNode(name) {
					CreationTimeUtc = entry.CreationTimeUtc,
					LastAccessTimeUtc = entry.LastAccessTimeUtc,
					LastWriteTimeUtc = entry.LastWriteTimeUtc
				};

				PopulateChildren(directory, entry, entries, names, fileData, dataStartOffset);
				node = directory;
			} else {
				var offset = checked((int)(dataStartOffset + entry.DataOffset));
				var length = checked((int)entry.UncompressedSize);
				var bytes = fileData.Slice(offset, length).ToArray();
				node = new UtfFileNode(name, bytes) {
					Attributes = entry.Attributes,
					CreationTimeUtc = entry.CreationTimeUtc,
					LastAccessTimeUtc = entry.LastAccessTimeUtc,
					LastWriteTimeUtc = entry.LastWriteTimeUtc
				};
			}

			parent.Children[name] = node;
			if (entry.NextOffset == 0) {
				break;
			}

			nextEntryIndex = (int)(entry.NextOffset / DirectoryEntrySize);
		}
	}

	private static void AddNodes(UtfDirectoryNode parent, UtfEntryPlan parentPlan, List<UtfEntryPlan> plans) {
		foreach (var child in parent.Children.Values) {
			var plan = new UtfEntryPlan(child) { Index = plans.Count };
			plans.Add(plan);
			parentPlan.Children.Add(plan);

			if (child is UtfDirectoryNode directory) {
				AddNodes(directory, plan, plans);
			}
		}
	}

	private static Dictionary<string, uint> BuildNameBuffer(
		IReadOnlyCollection<string> names,
		out byte[] buffer,
		out int nameSpaceUsed) {
		var result = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase) {
			[UtfConstants.RootName] = 1
		};

		using var stream = new MemoryStream();
		stream.WriteByte(0);
		stream.WriteByte((byte)'\\');
		stream.WriteByte(0);

		foreach (var name in names.Where(name =>
			         !string.Equals(name, UtfConstants.RootName, StringComparison.OrdinalIgnoreCase))) {
			result[name] = (uint)stream.Position;
			var bytes = System.Text.Encoding.ASCII.GetBytes(name);
			stream.Write(bytes, 0, bytes.Length);
			stream.WriteByte(0);
		}

		stream.WriteByte(0);
		nameSpaceUsed = (int)stream.Length;
		while ((stream.Length & 3) != 0) {
			stream.WriteByte(0);
		}

		buffer = stream.ToArray();
		return result;
	}

	private static string GetName(byte[] names, uint offset) {
		var index = checked((int)offset);
		var length = 0;
		while (index + length < names.Length && names[index + length] != 0) {
			length++;
		}

		return System.Text.Encoding.ASCII.GetString(names, index, length);
	}

	private static int Align4(int value) {
		return (value + 3) & ~3;
	}

	private sealed class UtfEntryPlan {
		public UtfEntryPlan(UtfNode node) {
			Node = node;
		}

		public int Index { get; set; }

		public UtfNode Node { get; }

		public uint EntryOffset { get; set; }

		public uint NameOffset { get; set; }

		public uint DataOffset { get; set; }

		public uint DataLength { get; set; }

		public uint NextOffset { get; set; }

		public List<UtfEntryPlan> Children { get; } = [];
	}

	private readonly record struct UtfEntryRecord(
		uint NextOffset,
		uint NameOffset,
		FileAttributes Attributes,
		uint DataOffset,
		uint SpaceAllocated,
		uint SpaceUsed,
		uint UncompressedSize,
		DateTime CreationTimeUtc,
		DateTime LastAccessTimeUtc,
		DateTime LastWriteTimeUtc) {
		public static UtfEntryRecord Read(BinaryReader reader, uint dataStartOffset, uint dirEntrySize) {
			var nextOffset = reader.ReadUInt32();
			var nameOffset = reader.ReadUInt32();
			var attributes = (FileAttributes)reader.ReadUInt32();
			_ = reader.ReadUInt32();
			var dataOffset = reader.ReadUInt32();
			var spaceAllocated = reader.ReadUInt32();
			var spaceUsed = reader.ReadUInt32();
			var uncompressedSize = reader.ReadUInt32();
			var creation = ReadDosDateTime(reader.ReadUInt32());
			var access = ReadDosDateTime(reader.ReadUInt32());
			var write = ReadDosDateTime(reader.ReadUInt32());

			var consumed = 44u;
			if (dirEntrySize > consumed) {
				reader.BaseStream.Position += dirEntrySize - consumed;
			}

			return new UtfEntryRecord(
				nextOffset,
				nameOffset,
				attributes,
				dataOffset,
				spaceAllocated,
				spaceUsed,
				uncompressedSize,
				creation,
				access,
				write);
		}

		public static void Write(BinaryWriter writer, UtfEntryPlan plan, int dataStartOffset) {
			writer.Write(plan.NextOffset);
			writer.Write(plan.NameOffset);
			writer.Write((uint)plan.Node.Attributes);
			writer.Write(0u);
			writer.Write(plan.DataOffset);
			writer.Write(plan.DataLength);
			writer.Write(plan.DataLength);
			writer.Write(plan.DataLength);
			writer.Write(WriteDosDateTime(plan.Node.CreationTimeUtc));
			writer.Write(WriteDosDateTime(plan.Node.LastAccessTimeUtc));
			writer.Write(WriteDosDateTime(plan.Node.LastWriteTimeUtc));
		}

		private static DateTime ReadDosDateTime(uint value) {
			if (value == 0) {
				return DateTime.UnixEpoch;
			}

			var date = (ushort)(value & 0xFFFF);
			var time = (ushort)(value >> 16);
			var year = 1980 + ((date >> 9) & 0x7F);
			var month = (date >> 5) & 0x0F;
			var day = date & 0x1F;
			var hour = (time >> 11) & 0x1F;
			var minute = (time >> 5) & 0x3F;
			var second = (time & 0x1F) * 2;
			return new DateTime(year, Math.Max(month, 1), Math.Max(day, 1), hour, minute, second, DateTimeKind.Utc);
		}

		private static uint WriteDosDateTime(DateTime value) {
			var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
			if (utc.Year < 1980) {
				utc = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			}

			var date = (ushort)(((utc.Year - 1980) << 9) | (utc.Month << 5) | utc.Day);
			var time = (ushort)((utc.Hour << 11) | (utc.Minute << 5) | (utc.Second / 2));
			return (uint)(date | (time << 16));
		}
	}
}
