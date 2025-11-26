<div class="mb-4">
    <label class="block font-medium text-sm text-gray-700 mb-1">Select Project</label>
    <select wire:model="projectId" class="block w-full border-gray-300 rounded-md shadow-sm">
        @foreach ($this->projects as $id => $name)
            <option value="{{ $id }}">{{ $name }}</option>
        @endforeach
    </select>
</div>
