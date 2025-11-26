<x-filament-panels::page>
    <div class="space-y-6">

        <!-- Header Grid -->
        <div class="grid grid-cols-2 items-center">
            <h1 class="text-2xl font-bold">Project Guideline</h1>

            <!-- Button aligned on the right -->
            <div class="flex justify-end">
                <x-filament::button
                    tag="a"
                    href="{{ route('guideline.download') }}"
                    icon="heroicon-o-arrow-down-tray"
                    size="sm"
                >
                    Download PDF
                </x-filament::button>
            </div>
        </div>

        <!-- PDF Viewer (full width, centered content) -->
        <div 
            class="border rounded-lg shadow bg-white p-4 grid place-items-center"
            style="height: 85vh;"
        >
            <iframe 
                src="{{ route('guideline.preview') }}#toolbar=0&navpanes=0&scrollbar=0&view=FitH"
                class="rounded-md"
                style="width: 55%; height: 90%; border:none;"
            ></iframe>
        </div>

    </div>
</x-filament-panels::page>
