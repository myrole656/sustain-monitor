<?php

namespace App\Filament\User\Widgets;

use Filament\Widgets\ChartWidget;
use App\Models\Project;
use App\Models\Process;
use Illuminate\Support\Facades\Auth;

class ResultChart extends ChartWidget
{
    protected bool $hasForm = false; // No form needed
    protected static bool $isFormHidden = true;

    public ?int $project_id = null;

    public function getHeading(): string
    {
        // Get the latest project of the user
        $project = Project::where('user_id', Auth::id())->latest()->first();

        $this->project_id = $project?->id;

        return $project?->project_name ?? 'Project Process Completion';
    }

    public function getType(): string
    {
        return 'doughnut';
    }

    public function getData(): array
    {
        $userId = Auth::id();

        $process = Process::query()
            ->when($this->project_id, fn($q) => $q->where('project_id', $this->project_id))
            ->when(!$this->project_id, fn($q) =>
                $q->whereHas('project', fn($p) => $p->where('user_id', $userId))
                    ->oldest()
            )
            ->first();

        // Maximum marks per category
        $maxMarks = [
            'Initiation' => 17,
            'Planning'   => 26,
            'Execution'  => 42,
            'Monitoring' => 12,
            'Closing'    => 14,
        ];

        // Step marks (0 if process not found)
        $stepMarks = $process ? [
            'Initiation' => $process->initiation,
            'Planning'   => $process->planning,
            'Execution'  => $process->execution,
            'Monitoring' => $process->monitoring,
            'Closing'    => $process->closing,
        ] : array_fill_keys(array_keys($maxMarks), 0);

        $totalMax = array_sum($maxMarks);          // 111 total marks
        $obtained = array_sum($stepMarks);         // total obtained by user
        $overallPercentage = round(($obtained / $totalMax) * 100, 1);

        // Prepare chart labels as "obtained / max"
        $labels = [];
        $percentages = [];
        foreach ($stepMarks as $step => $mark) {
            $labels[] = "{$step} ({$mark}/{$maxMarks[$step]})";
            $percentages[] = round(($mark / $totalMax) * 100, 1);
        }

        $remaining = max(0, 100 - array_sum($percentages));

        return [
            'labels' => array_merge($labels, ['Remaining']),
            'datasets' => [
                [
                    'label' => 'Project Completion (%)',
                    'data' => array_merge($percentages, [$remaining]),
                    'backgroundColor' => [
                        '#22c55e',
                        '#ef4444',
                        '#facc15',
                        '#3c2060',
                        '#0ea5e9',
                        '#e5e7eb',
                    ],
                    'hoverOffset' => 12,
                ],
            ],
            'overallPercentage' => $overallPercentage,
        ];
    }
}
