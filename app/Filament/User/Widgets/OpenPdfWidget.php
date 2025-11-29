<?php

namespace App\Filament\User\Widgets;

use Filament\Widgets\StatsOverviewWidget as BaseWidget;
use Filament\Widgets\StatsOverviewWidget\Stat;

class OpenPdfWidget extends BaseWidget
{
    protected function getStats(): array
    {
        return [
            Stat::make('BIM Guideline', 'Click to view')
                ->description('Open project PDF')
                ->color('success')
                ->extraAttributes([
                    'class' => 'cursor-pointer',
                    'onclick' => "window.open('/pdf/BIM_Guide.pdf', '_blank')",
                ]),
        ];
    }
}
