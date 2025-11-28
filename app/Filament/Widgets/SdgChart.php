<?php

namespace App\Filament\Widgets;

use Filament\Widgets\ChartWidget;

class SdgChart extends ChartWidget
{
    protected ?string $heading = 'SDG Question Overview';

    protected function getData(): array
    {
        // Maximum points for each SDG
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

        return [
            'labels' => [
                'SDG 3', 'SDG 6', 'SDG 7', 'SDG 8', 'SDG 9',
                'SDG 11', 'SDG 12', 'SDG 13', 'SDG 15',
            ],

            'datasets' => [
                [
                    'label' => 'Max Points',
                    'data' => array_values($sdgMaxPoints),
                ],
            ],
        ];
    }

    protected function getType(): string
    {
        return 'line';
    }
}
