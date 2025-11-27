<x-filament::page>

    {{-- WRAPPER --}}
    <div class="max-w-6xl mx-auto">

        {{-- Top Section --}}
        <section class="mb-8">
            <h1 class="text-3xl font-bold text-gray-800">Project Status Dashboard</h1>
            <p class="text-gray-500 mt-1">Step-by-step guide for users.</p>
        </section>

        {{-- Step Cards --}}
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">

            @php
                $steps = [
                    ['title' => 'Step 1', 'description' => 'Register an account'],
                    ['title' => 'Step 2', 'description' => 'Verify your email'],
                    ['title' => 'Step 3', 'description' => 'Complete your profile'],
                    ['title' => 'Step 4', 'description' => 'Create your first project'],
                    ['title' => 'Step 5', 'description' => 'Track your project status'],
                ];
            @endphp

            @foreach ($steps as $step)
                <div class="bg-white shadow-lg rounded-xl p-6 hover:shadow-xl transition-all duration-200 border border-gray-200">
                    <div class="flex flex-col items-center text-center space-y-3">
                        <span class="text-xl font-bold text-amber-500">{{ $step['title'] }}</span>
                        <p class="text-gray-700">{{ $step['description'] }}</p>
                    </div>
                </div>
            @endforeach

        </div>

        {{-- Bottom Section --}}
        <section class="mt-10 bg-white p-6 rounded-xl shadow-lg border border-gray-200">
            <h2 class="text-xl font-bold text-gray-800 mb-4">Additional Information</h2>

            <div class="space-y-2 text-gray-600">
                <p>Follow each step carefully to complete your tasks successfully.</p>
                <p>If you encounter any issues, contact support.</p>
            </div>
        </section>

    </div>

</x-filament::page>
