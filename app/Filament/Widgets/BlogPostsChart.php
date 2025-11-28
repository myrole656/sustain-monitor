<?php

namespace App\Filament\Widgets;

use Filament\Widgets\ChartWidget;
use App\Models\Process;

class BlogPostsChart extends ChartWidget
{
    protected ?string $heading = 'Project Status Overview';

    protected function getData(): array
    {
        // Count projects grouped by status
        $statusCounts = Process::select('status')
            ->selectRaw('COUNT(*) as total')
            ->groupBy('status')
            ->pluck('total', 'status')
            ->toArray();

        // Prepare chart labels and values
        $labels = array_keys($statusCounts);      // e.g. ['platinum', 'gold', 'silver']
        $values = array_values($statusCounts);    // e.g. [5, 10, 7]

        return [
            'datasets' => [
                [
                    'label' => 'Project Count',
                    'data' => $values,
                    'fill' => false,
                ],
            ],
            'labels' => $labels,
        ];
    }

    protected function getType(): string
    {
        return 'bar';
    }
}
