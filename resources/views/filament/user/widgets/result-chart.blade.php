<div>
    <div class="text-center mb-2 font-semibold">
        Overall Completion: {{ $this->data['overallPercentage'] ?? 0 }}%
    </div>
    {{ $this->chart }}
</div>
