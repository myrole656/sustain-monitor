<?php

namespace App\Filament\User\Widgets;

use App\Models\Project;
use App\Models\SDGStatus;
use Filament\Widgets\StatsOverviewWidget;
use Filament\Widgets\StatsOverviewWidget\Stat;
use Illuminate\Support\Facades\Auth;

class SDG extends StatsOverviewWidget
{
    protected ?string $heading = 'SDG Progress';

   public ?int $projectId = null;

    // Listen to dropdown event
    protected $listeners = ['projectSelected' => 'updateProject'];


    public function updateProject($projectId)
    {
        $this->projectId = $projectId;
        $this->dispatch('$refresh');
    }



    protected function getStats(): array
    {
        $userId = Auth::id();

        // SDG maximum points
        $sdgMaxPoints = [
            'sdg3'  => 10,
            'sdg6'  => 4,
            'sdg7'  => 9,
            'sdg8'  => 7,
            'sdg9'  => 32,
            'sdg11' => 7,
            'sdg12' => 30,
            'sdg13' => 4,
            'sdg15' => 1,
        ];

        // Default SDG points if no project exists
        $sdgPoints = array_fill_keys(array_keys($sdgMaxPoints), 0);
        $status = 'N/A';

        // Get latest project of the user
       
        if ($this->projectId) {
            $sdgStatus = SDGStatus::where('project_id', $this->projectId)->first();
            if ($sdgStatus) {
                $sdgPoints = [
                    'sdg3'  => $sdgStatus->sdg3,
                    'sdg6'  => $sdgStatus->sdg6,
                    'sdg7'  => $sdgStatus->sdg7,
                    'sdg8'  => $sdgStatus->sdg8,
                    'sdg9'  => $sdgStatus->sdg9,
                    'sdg11' => $sdgStatus->sdg11,
                    'sdg12' => $sdgStatus->sdg12,
                    'sdg13' => $sdgStatus->sdg13,
                    'sdg15' => $sdgStatus->sdg15,
                ];
                $status = $sdgStatus->status ?? 'N/A';
            }
        }

        return [
            Stat::make('SDG 3: Good Health & Well-Being', "{$sdgPoints['sdg3']}/{$sdgMaxPoints['sdg3']}")
                ->description('Ensure healthy lives and promote well-being for all')
                ->descriptionIcon('heroicon-o-heart')
                ->color('success'),

            Stat::make('SDG 6: Clean Water & Sanitation', "{$sdgPoints['sdg6']}/{$sdgMaxPoints['sdg6']}")
                ->description('Ensure availability and sustainable management of water')
                ->descriptionIcon('heroicon-o-sparkles')
                ->color('info'),

            Stat::make('SDG 7: Affordable & Clean Energy', "{$sdgPoints['sdg7']}/{$sdgMaxPoints['sdg7']}")
                ->description('Ensure access to affordable and sustainable energy')
                ->descriptionIcon('heroicon-o-light-bulb')
                ->color('info'),

            Stat::make('SDG 8: Decent Work & Economic Growth', "{$sdgPoints['sdg8']}/{$sdgMaxPoints['sdg8']}")
                ->description('Promote sustained, inclusive economic growth')
                ->descriptionIcon('heroicon-o-chart-bar')
                ->color('success'),

            Stat::make('SDG 9: Industry, Innovation & Infrastructure', "{$sdgPoints['sdg9']}/{$sdgMaxPoints['sdg9']}")
                ->description('Build resilient infrastructure and foster innovation')
                ->descriptionIcon('heroicon-o-cube')
                ->color('primary'),

            Stat::make('SDG 11: Sustainable Cities & Communities', "{$sdgPoints['sdg11']}/{$sdgMaxPoints['sdg11']}")
                ->description('Make cities inclusive, safe, and sustainable')
                ->descriptionIcon('heroicon-o-building-library')
                ->color('warning'),

            Stat::make('SDG 12: Responsible Consumption & Production', "{$sdgPoints['sdg12']}/{$sdgMaxPoints['sdg12']}")
                ->description('Ensure sustainable consumption and production patterns')
                 ->descriptionIcon('heroicon-o-light-bulb')
                ->color('warning'),

            Stat::make('SDG 13: Climate Action', "{$sdgPoints['sdg13']}/{$sdgMaxPoints['sdg13']}")
                ->description('Take urgent action to combat climate change')
                 ->descriptionIcon('heroicon-o-light-bulb')
                ->color('danger'),

            Stat::make('SDG 15: Life on Land', "{$sdgPoints['sdg15']}/{$sdgMaxPoints['sdg15']}")
                ->description('Protect, restore and promote sustainable ecosystems')
                 ->descriptionIcon('heroicon-o-light-bulb')
                ->color('info'),

            Stat::make('Project Status', $status)
                ->description('Final grade of latest project')
                ->descriptionIcon('heroicon-o-light-bulb')
                ->color(match($status) {
                    'PLATINUM' => 'success',
                    'GOLD' => 'warning',
                    'SILVER' => 'primary',
                    'CERTIFIED' => 'info',
                    'FAIL' => 'danger',
                    default => 'secondary',
                }),
        ];
    }
}
